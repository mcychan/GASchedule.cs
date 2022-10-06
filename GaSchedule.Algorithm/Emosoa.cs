using System;
using System.Collections.Generic;
using System.Linq;
using GaSchedule.Model;

/*
 * Dhiman, Gaurav & Singh, Krishna & Slowik, Adam & Chang, Victor & Yildiz, Ali & Kaur, Amandeep & Garg, Meenakshi. (2021).
 * EMoSOA: A New Evolutionary Multi-objective Seagull Optimization Algorithm for Global Optimization.
 * International Journal of Machine Learning and Cybernetics. 12. 10.1007/s13042-020-01189-1.
 * Copyright (c) 2022 Miller Cy Chan
 */

namespace GaSchedule.Algorithm
{
	/****************** Evolutionary multi-objective seagull optimization algorithm (EMoSOA) **********************/
	public class Emosoa<T> : NsgaII<T> where T : Chromosome<T>
	{
		private int _currentGeneration = 0, _max_iterations = 5000;
		private float _gBestScore;
		private float[] _bestScore;
		private float[] _gBest = null;
		private float[][] _current_position = null;

		// Initializes Evolutionary multi-objective seagull optimization algorithm
		public Emosoa(T prototype, int numberOfCrossoverPoints = 2, int mutationSize = 2, float crossoverProbability = 80, float mutationProbability = 3) : base(prototype, numberOfCrossoverPoints, mutationSize, crossoverProbability, mutationProbability)
		{
		}

		static E[][] CreateArray<E>(int rows, int cols)
		{
			E[][] array = new E[rows][];
			for (int i = 0; i < array.GetLength(0); i++)
				array[i] = new E[cols];

			return array;
		}

		private void Exploitation(List<T> population)
		{
			int b = 1;
			var Fc = 2f - _currentGeneration * (2f / _max_iterations);
			var tau = 2 * Math.PI;
			for (int i = 0; i < population.Count; ++i)
			{
				int dim = _current_position[i].Length;
				for (int j = 0; j < dim; ++j)
				{
					var A1 = 2 * Fc * Configuration.Random() - Fc;
					var ll = (Fc - 1) * Configuration.Random() + 1;

					var D_alphs = Fc * _current_position[i][j] + A1 * (_gBest[j] - _current_position[i][j]);
					_current_position[i][j] = (float)(D_alphs * Math.Exp(b * ll) * Math.Cos(ll * tau) + _gBest[j]);
				}
			}
		}

		protected override List<T> Replacement(List<T> population)
		{
			var populationSize = population.Count;
			var climax = .9f;

			for (int i = 0; i < populationSize; ++i)
			{
				var fitness = population[i].Fitness;
				if (fitness < _bestScore[i])
				{
					population[i].UpdatePositions(_current_position[i]);
					fitness = population[i].Fitness;
				}

				if (fitness > _bestScore[i])
				{
					_bestScore[i] = fitness;
					population[i].ExtractPositions(_current_position[i]);
				}

				if (fitness > _gBestScore)
				{
					_gBestScore = fitness;
					population[i].ExtractPositions(_current_position[i]);
					_gBest = _current_position[i].ToArray();
				}

				if (_repeatRatio > climax && _gBestScore > climax) {
					if (i > (populationSize * _repeatRatio))
						population[i].UpdatePositions(_current_position[i]);
				}
			}

			Exploitation(population);
			return base.Replacement(population);
		}


		protected override void Initialize(List<T> population)
		{
			int size = 0;
			int numberOfChromosomes = _populationSize;
			for (int i = 0; i < _populationSize; ++i)
			{
				List<float> positions = new();

				// add new search agent to population
				population.Add(_prototype.MakeNewFromPrototype(positions));

				if (i < 1)
				{
					size = positions.Count;
					_current_position = CreateArray<float>(numberOfChromosomes, size);
					_gBest = new float[numberOfChromosomes];
					_bestScore = new float[numberOfChromosomes];
				}

				_bestScore[i] = population[i].Fitness;
				for (int j = 0; j < size; ++j)
					_current_position[i][j] = positions[j];
			}
		}
		
				// Starts and executes algorithm
		public override void Run(int maxRepeat = 9999, double minFitness = 0.999)
		{
			if (_prototype == null)
				return;

			var population = new List<T>();
			Initialize(population);

			int repeat = 0;
			var lastBestFit = 0.0;

			while(_currentGeneration < _max_iterations)
			{
				var best = Result;
				if (_currentGeneration > 0)
				{
					var status = string.Format("\rFitness: {0:F6}\t Generation: {1}", best.Fitness, _currentGeneration);
					Console.Write(status);

					// algorithm has reached criteria?
					if (best.Fitness > minFitness)
						break;

					var difference = Math.Abs(best.Fitness - lastBestFit);
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
				if (_currentGeneration == 0)
					_chromosomes = population.ToArray();
				else
				{
					totalChromosome = new List<T>(population);
					totalChromosome.AddRange(_chromosomes);
					var newBestFront = NonDominatedSorting(totalChromosome);
					_chromosomes = Selection(newBestFront, totalChromosome).ToArray();
					lastBestFit = best.Fitness;
				}
				++_currentGeneration;
			}
		}

		public override string ToString()
		{
			return "Evolutionary multi-objective seagull optimization algorithm for global optimization (EMoSOA)";
		}
	}
}
