using System;
using System.Collections.Generic;
using System.Linq;
using GaSchedule.Model;

/*
 * Xie, Jian & Chen, Huan. (2013).
 * A Novel Bat Algorithm Based on Differential Operator and Lévy Flights Trajectory.
 * Computational intelligence and neuroscience. 2013. 453812. 10.1155/2013/453812. 
 * Copyright (c) 2024 Miller Cy Chan
 */

namespace GaSchedule.Algorithm
{
	public class Dlba<T> : NsgaIII<T> where T : Chromosome<T>
	{
		private int _currentGeneration, _max_iterations = 5000;
		
		private int _chromlen, _minValue = 0;
		
		private double _alpha, _pa;

		private float[] _loudness, _rate;

		private float[] _gBest = null;
		private float[][] _position = null;

		private List<int> _maxValues;
		private LévyFlights<T> _lf;

		// Initializes Bat algorithm
		public Dlba(T prototype, int numberOfCrossoverPoints = 2, int mutationSize = 2, float crossoverProbability = 80, float mutationProbability = 3) : base(prototype, numberOfCrossoverPoints, mutationSize, crossoverProbability, mutationProbability)
		{
			// there should be at least 5 chromosomes in population
			if (_populationSize < 5)
				_populationSize = 5;

			_alpha = 0.9;
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
			_maxValues = new();
			_prototype.MakeEmptyFromPrototype(_maxValues);
			
			for (int i = 0; i < _populationSize; ++i) {
				List<float> positions = new();

				// initialize new population with chromosomes randomly built using prototype
				population.Add(_prototype.MakeNewFromPrototype(positions));
				
				if(i < 1) {
					_chromlen = positions.Count;
					_rate = new float[_populationSize];
					_loudness = new float[_populationSize];
					_position = CreateArray<float>(_populationSize, _chromlen);
					_lf = new LévyFlights<T>(_chromlen);
				}
				
				_rate[i] = (float) Configuration.Random();
				_loudness[i] = (float) Configuration.Random() + 1;
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
			var mean = _loudness.Average();
			if(_gBest == null)
				_gBest = _position[0];
			var prevBest = _prototype.MakeEmptyFromPrototype();
			prevBest.UpdatePositions(_gBest);

			for (int i = 0; i < _populationSize; ++i) {
				var beta = (float) Configuration.Random();
				var rand = Configuration.Random();
				var B1 = Configuration.Rand(-1.0, 1.0);
				var B2 = Configuration.Rand(-1.0, 1.0);

				int r1 = Configuration.Rand(_populationSize);
				int r2 = Configuration.Rand(_populationSize);
				while(r1 == r2)
					r2 = Configuration.Rand(_populationSize);
				int r3 = Configuration.Rand(_populationSize);
				int r4 = Configuration.Rand(_populationSize);
				while(r3 == r4)
					r4 = Configuration.Rand(_populationSize);

				int dim = _position[i].Length;
				for(int j = 0; j < dim; ++j) {
					var f1 = ((_minValue - _maxValues[j]) * _currentGeneration / (float) B1 + _maxValues[j]) * beta;
					var f2 = ((_maxValues[j] - _minValue) * _currentGeneration / (float) B2 + _minValue) * beta;
					_position[i][j] = _gBest[j] + f1 * (_position[r1][j] - _position[r2][j]) + f2 * (_position[r3][j] - _position[r3][j]);
					
					if (rand > _rate[i]) {
						var e = Configuration.Rand(-1.0, 1.0);
						_position[i][j] += (float) (_gBest[j] + e * mean);
					}
				}

				_gBest = _lf.UpdatePosition(population[i], _position, i, _gBest);
			}

			var globalBest = _prototype.MakeEmptyFromPrototype(null);
			globalBest.UpdatePositions(_gBest);
			mean = _rate.Average();
			for (int i = 0; i < _populationSize; ++i) {
				var positionTemp = _position.ToArray();
				var rand = Configuration.Random();
				if (rand < _loudness[i]) {
					var n = Configuration.Rand(-1.0, 1.0);
					int dim = _position[i].Length;
					for(int j = 0; j < dim; ++j)
						positionTemp[i][j] = _gBest[j] + (float) n * mean;

					if (prevBest.Dominates(globalBest)) {
						_position[i] = positionTemp[i];
						_rate[i] *= (float) Math.Pow(_currentGeneration / n, 3);
						_loudness[i] *= (float) _alpha;
					}
				}
				
				_position[i] = _lf.Optimum(_position[i], population[i]);
			}
		}

		protected override List<T> Replacement(List<T> population)
		{
			UpdatePositions(population);
			
			for (int i = 0; i < _populationSize; ++i) {
				var chromosome = _prototype.MakeEmptyFromPrototype();
				chromosome.UpdatePositions(_position[i]);
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
			return "Bat algorithm with differential operator and Levy flights trajectory (DLBA)";
		}
	}
}
