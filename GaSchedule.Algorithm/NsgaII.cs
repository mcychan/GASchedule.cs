using System;
using System.Collections.Generic;
using System.Linq;

namespace GaSchedule.Algorithm
{
	// NSGA II
	public class NsgaII<T> where T : Chromosome<T>
	{
		// Population of chromosomes
		private List<T> _chromosomes;

		// Prototype of chromosomes in population
		private T _prototype;

		// Number of chromosomes
		private int _populationSize;

		// Number of crossover points of parent's class tables
		private int _numberOfCrossoverPoints;

		// Number of classes that is moved randomly by single mutation operation
		private int _mutationSize;

		// Probability that crossover will occur
		private float _crossoverProbability;

		// Probability that mutation will occur
		private float _mutationProbability;

		// Initializes NsgaII
		private NsgaII(T prototype, int numberOfChromosomes)
		{
			_prototype = prototype;
			// there should be at least 2 chromosomes in population
			if (numberOfChromosomes < 2)
				numberOfChromosomes = 2;
			_populationSize = numberOfChromosomes;
		}

		public NsgaII(T prototype, int numberOfCrossoverPoints = 2, int mutationSize = 2, float crossoverProbability = 80, float mutationProbability = 3) : this(prototype, 100)
		{
			_mutationSize = mutationSize;
			_numberOfCrossoverPoints = numberOfCrossoverPoints;
			_crossoverProbability = crossoverProbability;
			_mutationProbability = mutationProbability;
		}

		// Returns pointer to best chromosomes in population
		public T Result => (_chromosomes == null) ? default(T) : _chromosomes[0];

		/************** non-dominated sorting function ***************************/
		private List<HashSet<int> > NonDominatedSorting(List<T> totalChromosome)
		{
			var s = new HashSet<int>[_populationSize * 2];
			var n = new int[s.Length];
			var front = new List<HashSet<int> >();
			var rank = new int[s.Length];
			front.Add(new HashSet<int>());

			for (int p = 0; p < s.Length; ++p)
			{
				s[p] = new HashSet<int>();
				for (int q = 0; q < s.Length; ++q)
				{
					if (totalChromosome[p].Fitness > totalChromosome[q].Fitness)
						s[p].Add(q);
					else if (totalChromosome[p].Fitness < totalChromosome[q].Fitness)
						++n[p];
				}

				if (n[p] == 0)
				{
					rank[p] = 0;
					front[0].Add(p);
				}
			}

			int i = 0;
			while (front[i] != null && front[i].Any())
			{
				var Q = new HashSet<int>();
				foreach (int p in front[i])
				{
					foreach (int q in s[p])
					{
						if (--n[q] == 0)
						{
							rank[q] = i + 1;
							Q.Add(q);
						}
					}
				}
				++i;
				front.Add(Q);
			}
			return front.GetRange(0, front.Count - 1);
		}

		/************** calculate crowding distance function ***************************/
		private HashSet<int> CalculateCrowdingDistance(HashSet<int> front, List<T> totalChromosome)
		{
			var distance = front.ToDictionary(m => m, m => 0.0f);
			var obj = front.ToDictionary(m => m, m => totalChromosome[m].Fitness);

			var sortedKeys = obj.OrderBy(e => e.Value).Select(e => e.Key).ToArray();
			distance[sortedKeys[front.Count - 1]] = float.MaxValue;
			distance[sortedKeys[0]] = float.MaxValue;

			var values = new HashSet<float>(obj.Values);
			if (values.Count > 1)
			{
				for (int i = 1; i < front.Count - 1; ++i)
					distance[sortedKeys[i]] = distance[sortedKeys[i]] + (obj[sortedKeys[i + 1]] - obj[sortedKeys[i - 1]]) / (obj[sortedKeys[front.Count - 1]] - obj[sortedKeys[0]]);
			}
			return distance.OrderBy(e => e.Value).Select(e => e.Key).Reverse().ToHashSet();
		}

		private List<T> Selection(List<HashSet<int> > front, List<T> totalChromosome)
		{
			int N = 0;
			var newPop = new List<int>();
			while (N < _populationSize)
			{
				foreach (var row in front)
				{
					N += row.Count;
					if (N > _populationSize)
					{
						var sortedCdf = CalculateCrowdingDistance(row, totalChromosome);
						foreach (int j in sortedCdf)
						{
							if (newPop.Count >= _populationSize)
								break;
							newPop.Add(j);
						}
						break;
					}
					newPop.AddRange(row);
				}
			}

			return newPop.Select(n => totalChromosome[n]).ToList();
		}

		protected void Initialize(List<T> population)
		{
			// initialize new population with chromosomes randomly built using prototype
			for (int i = 0; i < _populationSize; ++i)
				population.Add(_prototype.MakeNewFromPrototype());
		}

		// Starts and executes algorithm
		public void Run(int maxRepeat = 9999, double minFitness = 0.999)
		{
			if (_prototype == null)
				return;

			var population = new List<T>();
			Initialize(population);

			// Current generation
			int currentGeneration = 0;
			int repeat = 0;
			double lastBestFit = 0.0;

			for (; ; )
			{
				var best = Result;
				if (currentGeneration > 0)
				{
					var status = string.Format("\rFitness: {0:F6}\t Generation: {1}", best.Fitness, currentGeneration);
					Console.Write(status);

					// algorithm has reached criteria?
					if (best.Fitness > minFitness)
						break;

					double difference = Math.Abs(best.Fitness - lastBestFit);
					if (difference <= 0.0000001)
						++repeat;
					else
						repeat = 0;

					if (repeat > (maxRepeat / 100))
						++_crossoverProbability;
				}

				/******************* crossover *****************/
				var offspring = new List<T>();
				Random rnd = new Random();
				var S = Enumerable.Range(0, _populationSize).OrderBy(_ => rnd.Next()).ToList();

				int halfPopulationSize = _populationSize / 2;
				for (int m = 0; m < halfPopulationSize; ++m)
				{
					var parent0 = population[S[2 * m]];
					var parent1 = population[S[2 * m + 1]];
					var child0 = parent0.Crossover(parent1, _numberOfCrossoverPoints, _crossoverProbability);
					var child1 = parent1.Crossover(parent0, _numberOfCrossoverPoints, _crossoverProbability);
					offspring.Add(child0);
					offspring.Add(child1);
				}

				/******************* mutation *****************/
				foreach (var child in offspring)
					child.Mutation(_mutationSize, _mutationProbability);

				var totalChromosome = new List<T>(population);
				totalChromosome.AddRange(offspring);

				/******************* non-dominated sorting *****************/
				var front = NonDominatedSorting(totalChromosome);

				/******************* selection *****************/
				population = Selection(front, totalChromosome);
				_populationSize = population.Count;

				/******************* comparison *****************/
				if (currentGeneration == 0)
					_chromosomes = population;
				else
				{
					totalChromosome = new List<T>(population);
					totalChromosome.AddRange(_chromosomes);
					var newBestFront = NonDominatedSorting(totalChromosome);
					_chromosomes = Selection(newBestFront, totalChromosome);
					lastBestFit = best.Fitness;
				}
				++currentGeneration;
			}
		}
	}

}

