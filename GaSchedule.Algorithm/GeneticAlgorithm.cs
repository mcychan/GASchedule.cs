using System;

namespace GaSchedule.Algorithm
{
    // Genetic algorithm
    public class GeneticAlgorithm<T> where T : Chromosome<T>
	{
		// Population of chromosomes
		private T[] _chromosomes;

		// Inidicates whether chromosome belongs to best chromosome group
		private bool[] _bestFlags;

		// Indices of best chromosomes
		private int[] _bestChromosomes;

		// Number of best chromosomes currently saved in best chromosome group
		private int _currentBestSize;

		// Number of chromosomes which are replaced in each generation by offspring
		private int _replaceByGeneration;		

		// Prototype of chromosomes in population
		private T _prototype;

		// Number of crossover points of parent's class tables
		private int _numberOfCrossoverPoints;

		// Number of classes that is moved randomly by single mutation operation
		private int _mutationSize;

		// Probability that crossover will occurr
		private float _crossoverProbability;

		// Probability that mutation will occurr
		private float _mutationProbability;				

		// Initializes genetic algorithm
		private GeneticAlgorithm(T prototype, int numberOfChromosomes, int replaceByGeneration, int trackBest)
        {
			_replaceByGeneration = replaceByGeneration;
			_currentBestSize = 0;
			_prototype = prototype;

			// there should be at least 2 chromosomes in population
			if (numberOfChromosomes < 2)
				numberOfChromosomes = 2;

			// and algorithm should track at least on of best chromosomes
			if (trackBest < 1)
				trackBest = 1;

			// reserve space for population
			_chromosomes = new T[numberOfChromosomes];
			_bestFlags = new bool[numberOfChromosomes];

			// reserve space for best chromosome group
			_bestChromosomes = new int[trackBest];

			ReplaceByGeneration = replaceByGeneration;
		}

		public GeneticAlgorithm(T prototype, int numberOfCrossoverPoints = 2, int mutationSize = 2, float crossoverProbability = 80, float mutationProbability = 3) : this(prototype, 100, 8, 5)
		{			
			_mutationSize = mutationSize;
			_numberOfCrossoverPoints = numberOfCrossoverPoints;
			_crossoverProbability = crossoverProbability;
			_mutationProbability = mutationProbability;
		}
		
		private int ReplaceByGeneration
        {
			set
            {
				int numberOfChromosomes = _chromosomes.Length;
				int trackBest = _bestChromosomes.Length;
				if (value > numberOfChromosomes - trackBest)
					value = numberOfChromosomes - trackBest;
				_replaceByGeneration = value;
			}
        }

		// Returns pointer to best chromosomes in population
		public T Result => _chromosomes[_bestChromosomes[0]];

        // Tries to add chromosomes in best chromosome group
        private void AddToBest(int chromosomeIndex)
        {
			// don't add if new chromosome hasn't fitness big enough for best chromosome group
			// or it is already in the group?
			if ((_currentBestSize == _bestChromosomes.Length &&
				_chromosomes[_bestChromosomes[_currentBestSize - 1]].Fitness >=
				_chromosomes[chromosomeIndex].Fitness) || _bestFlags[chromosomeIndex])
				return;

			// find place for new chromosome
			int i = _currentBestSize;
			for (; i > 0; i--)
			{
				// group is not full?
				if (i < _bestChromosomes.Length)
				{
					// position of new chromosomes is found?
					if (_chromosomes[_bestChromosomes[i - 1]].Fitness > _chromosomes[chromosomeIndex].Fitness)
						break;

					// move chromosomes to make room for new
					_bestChromosomes[i] = _bestChromosomes[i - 1];
				}
				else
					// group is full remove worst chromosomes in the group
					_bestFlags[_bestChromosomes[i - 1]] = false;
			}

			// store chromosome in best chromosome group
			_bestChromosomes[i] = chromosomeIndex;
			_bestFlags[chromosomeIndex] = true;

			// increase current size if it has not reached the limit yet
			if (_currentBestSize < _bestChromosomes.Length)
				_currentBestSize++;
		}		

		// Returns TRUE if chromosome belongs to best chromosome group
		private bool IsInBest(int chromosomeIndex)
        {
			return _bestFlags[chromosomeIndex];
		}

		// Clears best chromosome group
		private void ClearBest()
        {
			_bestFlags = new bool[_bestFlags.Length];
			_currentBestSize = 0;
		}

		protected void Initialize(T[] population)
		{
			// initialize new population with chromosomes randomly built using prototype
			for (int i = 0; i < population.Length; ++i)
			{
				population[i] = _prototype.MakeNewFromPrototype();
				// AddToBest(i);
			}
		}

		protected T[] Selection(T[] population)
        {
			// selects parent randomly
			var p1 = population[Configuration.Rand() % population.Length];
			var p2 = population[Configuration.Rand() % population.Length];
			return new T[] { p1, p2 };
		}

		protected T[] Replacement(T[] population)
        {
			// produce offspring
			var offspring = new T[_replaceByGeneration];
			for (int j = 0; j < _replaceByGeneration; j++)
			{
				var parent = Selection(population);

				offspring[j] = parent[0].Crossover(parent[1], _numberOfCrossoverPoints, _crossoverProbability);
				offspring[j].Mutation(_mutationSize, _mutationProbability);

				// replace chromosomes of current operation with offspring
				int ci;
				do
				{
					// select chromosome for replacement randomly
					ci = Configuration.Rand() % population.Length;

					// protect best chromosomes from replacement
				} while (IsInBest(ci));

				// replace chromosomes
				population[ci] = offspring[j];

				// try to add new chromosomes in best chromosome group
				AddToBest(ci);
			}
			return offspring;
		}

		// Starts and executes algorithm
		public void Run(int maxRepeat = 9999, double minFitness = 0.999)
		{
			if (_prototype == null)
				return;

			// clear best chromosome group from previous execution
			ClearBest();
			Initialize(_chromosomes);

			// Current generation
			int currentGeneration = 0;
			int repeat = 0;
			double lastBestFit = 0.0;
			for (; ; )
			{
				var best = Result;
				var status = string.Format("\rFitness: {0:F6}\t Generation: {1}", best.Fitness, currentGeneration++);
				Console.Write(status);				

				// algorithm has reached criteria?
				if (best.Fitness > minFitness)
					break;

				var difference = Math.Abs(best.Fitness - lastBestFit);
				if (difference <= 0.0000001)
					++repeat;
				else
					repeat = 0;

				if (repeat > (maxRepeat / 100))
				{
					ReplaceByGeneration = _replaceByGeneration * 3;
					++_crossoverProbability;
				}				

				Replacement(_chromosomes);

				lastBestFit = best.Fitness;
			}
		}

		public override string ToString()
		{
			return "Genetic Algorithm";
		}
	}
}
