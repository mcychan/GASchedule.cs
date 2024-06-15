using System;
using System.Collections.Generic;
using System.Linq;
using GaSchedule.Model;

/*
* Yang, X. S. 2012. Flower pollination algorithm for global optimization. Unconventional
* Computation and Natural Computation 7445: 240–49.
* Copyright (c) 2024 Miller Cy Chan
*/

namespace GaSchedule.Algorithm
{
	public class Fpa<T> : NsgaIII<T> where T : Chromosome<T>
	{
		private int _max_iterations = 5000;

		private int _chromlen;

		private double _pa;

		private float[] _gBest = null;

		private float[][] _current_position = null;

		private LévyFlights<T> _lf;

		// Initializes Flower Pollination Algorithm
		public Fpa(T prototype, int numberOfCrossoverPoints = 2, int mutationSize = 2, float crossoverProbability = 80, float mutationProbability = 3) : base(prototype, numberOfCrossoverPoints, mutationSize, crossoverProbability, mutationProbability)
		{
			_pa = .25;
		}

		static E[][] CreateArray<E>(int rows, int cols)
		{
			E[][] array = new E[rows][];
			for (int i = 0; i < array.GetLength(0); i++)
				array[i] = new E[cols];

			return array;
		}

		protected override void Initialize(List<T> population)
		{
			for (int i = 0; i < _populationSize; ++i) {
				List<float> positions = new();
				
				// initialize new population with chromosomes randomly built using prototype
				population.Add(_prototype.MakeNewFromPrototype(positions));
				
				if(i < 1) {
					_chromlen = positions.Count;
					_current_position = CreateArray<float>(_populationSize, _chromlen);
					_lf = new LévyFlights<T>(_chromlen);
				}
			}
		}

		protected override void Reform()
		{
			Configuration.Seed();
			if (_crossoverProbability < 95)
				_crossoverProbability += 1.0f;
			else if (_pa < .5)
				_pa += .01;
		}

		private void UpdatePositions(List<T> population)
		{
			var current_position = _current_position.ToArray();
			for (int i = 0; i < _populationSize; ++i) {
				var r = Configuration.Random();
				if(r < _pa)
					_gBest = _lf.UpdatePosition(population[i], _current_position, i, _gBest);
				else {
					int d1 = Configuration.Rand(_populationSize);
					int d2;
					do {
						d2 = Configuration.Rand(_populationSize);
					} while(d1 == d2);
					
					for(int j = 0; j < _chromlen; ++j)
						_current_position[i][j] += (float) (Configuration.Random() * (current_position[d1][j] - current_position[d2][j]));
				
					_current_position[i] = _lf.Optimum(_current_position[i], population[i]);
				}
			}
		}

		protected override List<T> Replacement(List<T> population)
		{
			UpdatePositions(population);
			
			for (int i = 0; i < _populationSize; ++i) {
				var chromosome = _prototype.MakeEmptyFromPrototype();
				chromosome.UpdatePositions(_current_position[i]);
				population[i] = chromosome;
			}

			return base.Replacement(population);
		}

		// Starts and executes algorithm
		public override void Run(int maxRepeat = 9999, double minFitness = 0.999)
		{
			if (_prototype == null)
				return;

			var pop = new List<T>[2];
			pop[0] = new List<T>();
			Initialize(pop[0]);

			// Current generation
			int currentGeneration = 0;
			int bestNotEnhance = 0;
			double lastBestFit = 0.0;

			int cur = 0, next = 1;
			while(currentGeneration < _max_iterations)
			{
				var best = Result;
				if (currentGeneration > 0)
				{
					var status = string.Format("\rFitness: {0:F6}\t Generation: {1}", best.Fitness, currentGeneration);
					Console.Write(status);

					// algorithm has reached criteria?
					if (best.Fitness > minFitness)
						break;

					var difference = Math.Abs(best.Fitness - lastBestFit);
					if (difference <= 1e-6)
						++bestNotEnhance;
					else {
						lastBestFit = best.Fitness;
						bestNotEnhance = 0;
					}

					if (bestNotEnhance > (maxRepeat / 100))
						Reform();
				}

				/******************* crossover *****************/
				var offspring = Crossing(pop[cur]);

				/******************* mutation *****************/
				foreach (var child in offspring)
					child.Mutation(_mutationSize, _mutationProbability);

				pop[cur].AddRange(offspring);

				/******************* replacement *****************/
				pop[next] = Replacement(pop[cur]);
				_best = pop[next][0].Dominates(pop[cur][0]) ? pop[next][0] : pop[cur][0];

				(cur, next) = (next, cur);
				++currentGeneration;
			}
		}

		public override string ToString()
		{
			return "Flower Pollination Algorithm (FPA)";
		}
	}
}