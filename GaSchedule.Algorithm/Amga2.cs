using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace GaSchedule.Algorithm
{
    /****************** Archive-based Micro Genetic Algorithm(AMGA2) **********************/
    public class Amga2<T> where T : Chromosome<T>
	{
		// Population of chromosomes
		private List<T> _archivePopulation, _parentPopulation, _offspringPopulation, _combinedPopulation;

		// Prototype of chromosomes in population
		protected T _prototype;

		private int _currentArchiveSize = 0;

		// Number of chromosomes
		protected int _populationSize, _archiveSize;

		// Index for crossover
		protected float _etaCross;

		// Number of classes that is moved randomly by single mutation operation
		private int _mutationSize;

		// Probability that crossover will occur
		protected float _crossoverProbability;

		// Probability that mutation will occur
		private float _mutationProbability;

		internal sealed class DistanceMatrix : IComparable<DistanceMatrix>
		{
			public int index1 = -1;
			public int index2 = -1;
			public float distance = 0.0f;

            public int CompareTo([AllowNull] DistanceMatrix other)
            {
				if (other == null)
					return 0;

				if (distance < other.distance)
					return -1;
				if (distance > other.distance)
					return 1;
				if (index1 < other.index1)
					return -1;
				if (index1 > other.index1)
					return 1;
				if (index2 < other.index2)
					return -1;
				if (index2 > other.index2)
					return 1;
				return 0;
			}
        }

		// Initializes Amga2
		private Amga2(T prototype, int numberOfChromosomes)
		{
			_prototype = prototype;
			// there should be at least 2 chromosomes in population
			if (numberOfChromosomes < 2)
				numberOfChromosomes = 2;
			_populationSize = _archiveSize = numberOfChromosomes;
		}

		public Amga2(T prototype, float etaCross = 0.35f, int mutationSize = 2, float crossoverProbability = 80, float mutationProbability = 3) : this(prototype, 100)
		{
			_mutationSize = mutationSize;
			_etaCross = etaCross;
			_crossoverProbability = crossoverProbability;
			_mutationProbability = mutationProbability;
		}

		// Returns pointer to best chromosomes in population
		public T Result => (_combinedPopulation == null) ? default(T) : _combinedPopulation[0];		
				
		protected void Initialize()
		{
			_archivePopulation = new List<T>();
			_parentPopulation = new List<T>();
			_offspringPopulation = new List<T>();
			_combinedPopulation = new List<T>();
			for (int i = 0; i < _archiveSize; ++i) {
				_archivePopulation.Add(_prototype.MakeNewFromPrototype());
				_combinedPopulation.Add(_prototype.MakeNewFromPrototype());
			}
			for (int i = 0; i < _populationSize; ++i) {
				_parentPopulation.Add(_prototype.MakeNewFromPrototype());
				_offspringPopulation.Add(_prototype.MakeNewFromPrototype());
				_combinedPopulation.Add(_prototype.MakeNewFromPrototype());
			}
		}

		private void AssignInfiniteDiversity(List<T> population, IEnumerable<int> elite)
		{
			foreach(var index in elite)
				population[index].Diversity = float.PositiveInfinity;		
		}

		private void AssignDiversityMetric(List<T> population, Queue<int> elite)
		{
			if (elite.Count <= 2)
            {
				AssignInfiniteDiversity(population, elite);
				return;
            }

			var distinct = ExtractDistinctIndividuals(population, elite);
			if (distinct.Count <= 2)
			{
				AssignInfiniteDiversity(population, elite);
				return;
			}
			
			int size = distinct.Count;

			distinct.ForEach(e => population[e].Diversity = 0.0f);

			var val = population[distinct[size - 1]].Fitness - population[distinct[0]].Fitness;
			if (val == 0)
				return;

			for (int j = 0; j < size; j++) {						
				if (j == 0)
				{
					var hashArray = new float[] { 0.0f, population[distinct[j]].Fitness, population[distinct[j + 1]].Fitness };
					var r = (hashArray[2] - hashArray[1]) / val;
					population[distinct[j]].Diversity += (r * r);
				}
				else if (j == size - 1)
				{
					var hashArray = new float[] { population[distinct[j - 1]].Fitness, population[distinct[j]].Fitness };
					var l = (hashArray[1] - hashArray[0]) / val;
					population[distinct[j]].Diversity += (l * l);
				}
				else
				{
					var hashArray = new float[] { population[distinct[j - 1]].Fitness, population[distinct[j]].Fitness, population[distinct[j + 1]].Fitness };
					var l = (hashArray[1] - hashArray[0]) / val;
					var r = (hashArray[2] - hashArray[1]) / val;
					population[distinct[j]].Diversity += (l * r);
				}
			}
		}

		private void CreateOffspringPopulation()
		{
			int r1, r2, r3;			
			for (int i = 0; i < _populationSize; ++i) {				
				do
				{
					r1 = Configuration.Rand(_currentArchiveSize);
				} while (_archivePopulation[r1].Equals(_archivePopulation[i]));
				do
				{
					r2 = Configuration.Rand(_currentArchiveSize);
				} while (_archivePopulation[r2].Equals(_archivePopulation[i]) || r2 == r1);
				do
				{
					r3 = Configuration.Rand(_currentArchiveSize);
				} while (_archivePopulation[r3].Equals(_archivePopulation[i]) || r3 == r1 || r3 == r2);
				_offspringPopulation[i] = _offspringPopulation[i].Crossover(_parentPopulation[i], _archivePopulation[r1], _archivePopulation[r2], _archivePopulation[r3], _etaCross, _crossoverProbability);
				_offspringPopulation[i].Rank = _parentPopulation[i].Rank; //for rank based mutation
			}
		}

		private int CheckDomination(T a, T b)
		{
			return a.Fitness.CompareTo(b.Fitness);
		}

		private List<int> ExtractDistinctIndividuals(List<T> population, Queue<int> elite)
		{
			return elite.Distinct().OrderBy(e => population[e].Fitness).ToList();
		}

		private List<int> ExtractENNSPopulation(List<T> mixedPopulation, Queue<int> pool, int desiredEliteSize)
		{
			int poolSize = pool.Count;
			int mixedSize = mixedPopulation.Count;
			var filtered = pool.Where(index => float.IsPositiveInfinity(mixedPopulation[index].Diversity)).Distinct().ToList();
			int numInf = filtered.Count;

			if (desiredEliteSize <= numInf)
				return filtered.Take(desiredEliteSize).ToList();

			var elite = pool.Distinct().ToList();
			pool.Clear();
			if (desiredEliteSize >= elite.Count)
				return elite;

			var distance = new float[poolSize, poolSize];
			var indexArray = new int[poolSize];
			var originalArray = new int[mixedSize];

			for (int i = 0; i < mixedSize; ++i)
				originalArray[i] = -1;

			int counter = 0;
			foreach (int index in elite)
			{
				indexArray[counter] = index;
				originalArray[indexArray[counter]] = counter++;
			}

			var distArray = new List<DistanceMatrix>();
			for (int i = 0; i < poolSize; ++i) {
				for (int j = i + 1; j < poolSize; ++j) {
                    var distMatrix = new DistanceMatrix
                    {
                        index1 = indexArray[i],
                        index2 = indexArray[j]
                    };
                    distance[j, i] = distance[i, j] = distMatrix.distance = Math.Abs(mixedPopulation[distMatrix.index1].Fitness - mixedPopulation[distMatrix.index2].Fitness);
					distArray.Add(distMatrix);
				}
			}

			distArray.Sort();
			int idx = 0;
			while (elite.Count > desiredEliteSize && idx < distArray.Count)
			{
				int index1, index2;
				do
				{
					var	temp = distArray[idx++];
					index1 = temp.index1;
					index2 = temp.index2;
				} while ((originalArray[index1] == -1 || originalArray[index2] == -1) && idx < distArray.Count);

				if (idx >= distArray.Count)
					break;

				if (float.IsPositiveInfinity(mixedPopulation[index1].Diversity) && float.IsPositiveInfinity(mixedPopulation[index2].Diversity))
					continue;
				
				if (float.IsPositiveInfinity(mixedPopulation[index1].Diversity))
				{
					elite.Remove(index2);
					pool.Enqueue(index2);
					originalArray[index2] = -1;
				}
				else if (float.IsPositiveInfinity(mixedPopulation[index2].Diversity))
				{
					elite.Remove(index1);
					pool.Enqueue(index1);
					originalArray[index1] = -1;
				}
				else
				{
					var dist1 = float.PositiveInfinity;
					foreach (int index in elite)
					{
						if (index != index1 && index != index2)
						{
							if (dist1 > distance[originalArray[index1], originalArray[index]])
								dist1 = distance[originalArray[index1], originalArray[index]];
						}
					}
					var dist2 = float.PositiveInfinity;
					foreach (int index in elite)
					{
						if (index != index1 && index != index2)
						{
							if (dist2 > distance[originalArray[index2], originalArray[index]])
								dist2 = distance[originalArray[index2], originalArray[index]];
						}
					}

					if (dist1 < dist2)
					{
						elite.Remove(index1);
						pool.Enqueue(index1);
						originalArray[index1] = -1;
					}
					else
					{
						elite.Remove(index2);
						pool.Enqueue(index2);
						originalArray[index2] = -1;
					}
				}
			}
			
			while (elite.Count > desiredEliteSize)
			{
				var temp = elite[0];
				pool.Enqueue(temp);
				elite.Remove(temp);
			}
			return elite;
		}

		private bool ExtractBestRank(List<T> population, Queue<int> pool, List<int> elite)
		{
			if (!pool.Any())
				return false;

			var remains = new List<int>();
			var index1 = pool.Dequeue();
			elite.Add(index1);			

			while (pool.Any())
			{
				index1 = pool.Dequeue();
				int flag = -1;
				int index2 = 0;
				while (index2 < elite.Count)
				{
					flag = CheckDomination(population[index1], population[index2]);
					if (flag == 1)
					{
						remains.Add(index2);
						elite.RemoveAt(index2);
					}
					else if (flag == -1)
						break;
					else
						++index2;
				}

				if (flag > -1)
					elite.Add(index1);
				else
					remains.Add(index1);
			}
			pool.Clear();
			remains.ForEach(r => pool.Enqueue(r));
			return true;
		}

		private void FillBestPopulation(List<T> mixedPopulation, int mixedLength, List<T> population, int populationLength)
		{
			var pool = new Queue<int>(Enumerable.Range(0, mixedLength));
			var elite = new Queue<int>();
			var filled = new List<int>();
			int rank = 1;

			foreach(var index in pool)
				mixedPopulation[index].Diversity = 0.0f;

			bool hasBetter = true;
			while (hasBetter && filled.Count < populationLength)
			{
				var elites = elite.ToList();
				hasBetter = ExtractBestRank(mixedPopulation, pool, elites);
				elites.ForEach(index => mixedPopulation[index].Rank = rank);

				elite = new Queue<int>(elites);
				if (rank++ == 1)
					AssignInfiniteDiversity(mixedPopulation, elite);

				if (elite.Count + filled.Count < populationLength)
				{
					filled.AddRange(elite);
					elite.Clear();
				}
				else
				{
					var temp = ExtractENNSPopulation(mixedPopulation, elite, populationLength - filled.Count);
					filled.AddRange(temp);
				}
			}

			int j = 0;
			filled.ForEach(index => population[j++] = mixedPopulation[index]);
		}

		private void FillDiversePopulation(List<T> mixedPopulation, Queue<int> pool, List<T> population, int startLocation, int desiredSize)
		{
			AssignDiversityMetric(mixedPopulation, pool);
			int poolSize = pool.Count;
			var indexArray = pool.OrderBy(e => mixedPopulation[e].Diversity).ToArray();

			for (int i = 0; i < desiredSize; ++i)
				population[startLocation + i] = mixedPopulation[indexArray[poolSize - 1 - i]];
		}

		private void CreateParentPopulation()
		{
			var pool = new Queue<int>(Enumerable.Range(0, _currentArchiveSize));
			var elite = new List<int>();
			var selectionPool = new Queue<int>();

			int rank = 1;
			while (selectionPool.Count < _populationSize)
			{
				ExtractBestRank(_archivePopulation, pool, elite);
				foreach (int i in elite)
				{
					_archivePopulation[i].Rank = rank;
					selectionPool.Enqueue(i);
				}
				++rank;
				elite.Clear();
			}

			int j = 0;
			foreach(var i in selectionPool)
				_parentPopulation[j++] = _archivePopulation[i];
			FillDiversePopulation(_archivePopulation, selectionPool, _parentPopulation, j, _populationSize - j);
		}

		private void MutateOffspringPopulation()
		{
			for (int i = 0; i < _populationSize; ++i) {				
				var pMut = _mutationProbability + (1.0f - _mutationProbability) * ((float)(_offspringPopulation[i].Rank - 1) / (_currentArchiveSize - 1)); //rank-based variation
				_offspringPopulation[i].Mutation(_mutationSize, pMut);
			}
		}

		private void UpdateArchivePopulation()
		{
			if (_currentArchiveSize + _populationSize <= _archiveSize)
			{
				for (int j = _currentArchiveSize, i = 0; i < _populationSize; ++i, ++j)
					_archivePopulation[j] = _offspringPopulation[i];

				_currentArchiveSize += _populationSize;
			}
			else
			{
				for (int i = 0; i < _currentArchiveSize; ++i)
					_combinedPopulation[i] = _archivePopulation[i];

				for (int i = 0; i < _populationSize; ++i)
					_combinedPopulation[_currentArchiveSize + i] = _offspringPopulation[i];

				FillBestPopulation(_combinedPopulation, _currentArchiveSize + _populationSize, _archivePopulation, _archiveSize);
				_currentArchiveSize = _archiveSize;
			}

			_archivePopulation.ForEach(e => e.Diversity = 0.0f);
		}

		private void FinalizePopulation()
		{
			var elite = new List<int>();
			var pool = new Queue<int>(Enumerable.Range(0, _currentArchiveSize).Where(i => _archivePopulation[i].Fitness >= 0.0));

			if (pool.Any())
			{
				ExtractBestRank(_archivePopulation, pool, elite);
				pool.Clear();
				if (elite.Count > _populationSize)
				{
					elite.ForEach(index => _archivePopulation[index].Diversity = 0.0f);

					AssignInfiniteDiversity(_archivePopulation, elite);
					ExtractENNSPopulation(_archivePopulation, pool, _populationSize);
					elite = pool.ToList();
				}
				_currentArchiveSize = elite.Count;
				int i = 0;
				elite.ForEach(index => _combinedPopulation[i++] = _archivePopulation[index]);
			}
			else
				_currentArchiveSize = 0;
		}

		// Starts and executes algorithm
		public void Run(int maxRepeat = 9999, double minFitness = 0.999)
		{
			if (_prototype == null)
				return;

			Initialize();
			_currentArchiveSize = _populationSize;

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
					{
						FinalizePopulation();
						break;
					}

					var difference = Math.Abs(best.Fitness - lastBestFit);
					if (difference <= 0.0000001)
						++repeat;
					else
						repeat = 0;

					if (repeat > (maxRepeat / 100))
						++_mutationProbability;
					lastBestFit = best.Fitness;
				}

				CreateParentPopulation();
				CreateOffspringPopulation();
				MutateOffspringPopulation();
				UpdateArchivePopulation();
				Configuration.Seed();
				++currentGeneration;
			}
		}

		public override string ToString()
		{
			return "Archive-based Micro Genetic Algorithm (AMGA2)";
		}
	}	
}
