using System;
using System.Collections.Generic;
using System.Linq;
using GaSchedule.Model;

/*
 * X. -S. Yang and Suash Deb, "Cuckoo Search via Lévy flights,"
 * 2009 World Congress on Nature & Biologically Inspired Computing (NaBIC), Coimbatore, India,
 * 2009, pp. 210-214, doi: 10.1109/NABIC.2009.5393690.
 * Copyright (c) 2023 Miller Cy Chan
 */

namespace GaSchedule.Algorithm
{
	/****************** Cuckoo Search Optimization (CSO) **********************/
	public class Cso<T> : NsgaIII<T> where T : Chromosome<T>
	{
		private int _max_iterations = 5000;
		private int _chromlen;
		private double _pa, _beta, _σu, _σv;
		private float[][] _current_position = null;

		// Initializes Cuckoo Search Optimization
		public Cso(T prototype, int numberOfCrossoverPoints = 2, int mutationSize = 2, float crossoverProbability = 80, float mutationProbability = 3) : base(prototype, numberOfCrossoverPoints, mutationSize, crossoverProbability, mutationProbability)
		{
			_pa = .25;
			_beta = 1.5;
			
			var num = Gamma(1 + _beta) * Math.Sin(Math.PI * _beta / 2);
			var den = Gamma((1 + _beta) / 2) * _beta * Math.Pow(2, (_beta - 1) / 2);
			_σu = Math.Pow(num / den, 1 / _beta);
			_σv = 1;
		}

		static E[][] CreateArray<E>(int rows, int cols)
		{
			E[][] array = new E[rows][];
			for (int i = 0; i < array.GetLength(0); i++)
				array[i] = new E[cols];

			return array;
		}
		
		static double Gamma(double z)
		{
			if (z < 0.5)
				return Math.PI / Math.Sin(Math.PI * z) / Gamma(1.0 - z);

			// Lanczos approximation g=5, n=7
			var coef = new double[7] { 1.000000000190015, 76.18009172947146, -86.50532032941677,
			24.01409824083091, -1.231739572450155, 0.1208650973866179e-2, -0.5395239384953e-5 };

			var zz = z - 1.0;
			var b = zz + 5.5; // g + 0.5
			var sum = coef[0];
			for (int i = 1; i < coef.Length; ++i)
				sum += coef[i] / (zz + i);

			var LogSqrtTwoPi = 0.91893853320467274178;
			return Math.Exp(LogSqrtTwoPi + Math.Log(sum) - b + Math.Log(b) * (zz + 0.5));
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
				}
			}
		}

		private float[] Optimum(float[] localVal, T chromosome)
		{
			var localBest = _prototype.MakeEmptyFromPrototype();
			localBest.UpdatePositions(localVal);
			
			if(localBest.Dominates(chromosome)) {
				chromosome.UpdatePositions(localVal);
				return localVal;
			}

			var positions = new float[_chromlen];
			chromosome.ExtractPositions(positions);
			return positions;
		}

		private void UpdatePosition1(List<T> population)
		{
			var current_position = _current_position.ToArray();
			for(int i = 0; i < _populationSize; ++i) {
				double u = Configuration.NextGaussian() * _σu;
				double v = Configuration.NextGaussian() * _σv;
				double S = u / Math.Pow(Math.Abs(v), 1 / _beta);
				float[] sBestScore = null;
				
				if(i == 0) {
					sBestScore = new float[_chromlen];
					population[i].ExtractPositions(sBestScore);
				}
				else
					sBestScore = Optimum(sBestScore, population[i]);

				for(int j = 0; j < _chromlen; ++j)
					_current_position[i][j] += (float) (Configuration.NextGaussian() * 0.01 * S * (current_position[i][j] - sBestScore[j]));

				_current_position[i] = Optimum(_current_position[i], population[i]);
			}
		}
		
		private void UpdatePosition2(List<T> population)
		{
			var current_position = _current_position.ToArray();
			for (int i = 0; i < _populationSize; ++i) {
				for(int j = 0; j < _chromlen; ++j) {
					var r = Configuration.Random();
					if(r < _pa) {
						int d1 = Configuration.Rand(5);
						int d2;
						do {
							d2 = Configuration.Rand(5);
						} while(d1 == d2);
						_current_position[i][j] += (float) (Configuration.Random() * (current_position[d1][j] - current_position[d2][j]));
					}
				}
				_current_position[i] = Optimum(_current_position[i], population[i]);
			}
		}

		protected override List<T> Replacement(List<T> population)
		{
			UpdatePosition1(population);
			UpdatePosition2(population);
			
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
			return "Cuckoo Search Optimization (CSO)";
		}
	}
}
