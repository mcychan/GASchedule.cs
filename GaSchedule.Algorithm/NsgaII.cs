using System;
using System.Collections.Generic;
using System.Linq;

using GaSchedule.Model;

/*
 * K.Deb, A.Pratap, S.Agrawal, T.Meyarivan, A fast and elitist multiobjective genetic algorithm: 
 * NSGA-II, IEEE Transactions on Evolutionary Computation 6 (2002) 182–197.
 * Copyright (c) 2020 - 2022 Miller Cy Chan
 */

namespace GaSchedule.Algorithm
{
	// NSGA II
	public class NsgaII<T> where T : Chromosome<T>
	{
		// Population of chromosomes
		protected T[] _chromosomes;

		// Prototype of chromosomes in population
		protected T _prototype;

		// Number of chromosomes
		protected int _populationSize;

		// Number of crossover points of parent's class tables
		protected int _numberOfCrossoverPoints;

		// Number of classes that is moved randomly by single mutation operation
		protected int _mutationSize;

		// Probability that crossover will occur
		protected float _crossoverProbability;

		// Probability that mutation will occur
		protected float _mutationProbability;
		
		protected float _repeatRatio;

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
		protected List<ISet<int> > NonDominatedSorting(List<T> totalChromosome)
		{
			var s = new HashSet<int>[_populationSize * 2];
			var n = new int[s.Length];
			var front = new List<ISet<int> >();
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
					front[0].Add(p);
			}

			int i = 0;
			while (front[i].Any())
			{
				var Q = new HashSet<int>();
				foreach (int p in front[i])
				{
					foreach (int q in s[p])
					{
						if (--n[q] == 0)
							Q.Add(q);
					}
				}
				++i;
				front.Add(Q);
			}

			front.RemoveAt(front.Count - 1);
			return front;
		}

		/************** calculate crowding distance function ***************************/
		private Dictionary<int, float> CalculateCrowdingDistance(ISet<int> front, List<T> totalChromosome)
		{
			var distance = new Dictionary<int, float>();
			var obj = new Dictionary<int, float>();

			foreach (var key in front)
			{
				distance[key] = 0.0f;
				var fitness = totalChromosome[key].Fitness;
				if(!obj.ContainsValue(fitness))
					obj[key] = fitness;

				var sortedKeys = obj.OrderBy(e => e.Value).Select(e => e.Key).ToArray();
				distance[sortedKeys[obj.Count - 1]] = float.MaxValue;
				distance[sortedKeys[0]] = float.MaxValue;

				if (obj.Count > 1)
				{
					var diff2 = totalChromosome[sortedKeys[obj.Count - 1]].GetDifference(totalChromosome[sortedKeys[0]]);

					for (int i = 1; i < obj.Count - 1; ++i)
					{
						var diff = totalChromosome[sortedKeys[i + 1]].GetDifference(totalChromosome[sortedKeys[i - 1]]) * 1.0f / diff2;
						distance[sortedKeys[i]] += diff;
					}
				}
			}
			return distance;
		}

		protected List<T> Selection(List<ISet<int> > front, List<T> totalChromosome)
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
						var distance = CalculateCrowdingDistance(row, totalChromosome);
						var sortedCdf = distance.OrderBy(e => e.Value).Select(e => e.Key).Reverse().Distinct().ToList();
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

		protected virtual List<T> Replacement(List<T> population)
		{
			var offspring = new List<T>();
			var rnd = new Random();
			var S = Enumerable.Range(0, _populationSize).OrderBy(_ => rnd.Next()).ToArray();

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
			return offspring;
		}
		protected virtual void Initialize(List<T> population)
		{
			// initialize new population with chromosomes randomly built using prototype
			for (int i = 0; i < _populationSize; ++i)
				population.Add(_prototype.MakeNewFromPrototype());
		}

		protected void Reform()
		{
			Configuration.Seed();
			if (_crossoverProbability < 95)
			_crossoverProbability += 1.0f;
			else if (_mutationProbability < 30)
			_mutationProbability += 1.0f;
		}

		// Starts and executes algorithm
		public virtual void Run(int maxRepeat = 9999, double minFitness = 0.999)
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

					_repeatRatio = repeat * 100.0f / maxRepeat;
					if (repeat > (maxRepeat / 100))
						Reform();

				}

				/******************* crossover *****************/
				var offspring = Replacement(population);

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
					_chromosomes = population.ToArray();
				else
				{
					totalChromosome = new List<T>(population);
					totalChromosome.AddRange(_chromosomes);
					var newBestFront = NonDominatedSorting(totalChromosome);
					_chromosomes = Selection(newBestFront, totalChromosome).ToArray();
					lastBestFit = best.Fitness;
				}
				++currentGeneration;
			}
		}

		public override string ToString()
		{
			return "NSGA II";
		}
	}
}

