using System;
using System.Collections.Generic;
using System.Linq;
using GaSchedule.Model;

/*
 * Zhang, G.X., Rong, H.N., Real-observation quantum-inspired evolutionary algorithm
 * for a class of numerical optimization problems. In: Lecture Notes
 * in Computer Science, vol. 4490, pp. 989–996 (2007).
 * Copyright (c) 2023 Miller Cy Chan
 */

namespace GaSchedule.Algorithm
{
	/****************** Real observation QIEA (rQIEA) **********************/
	public class Rqiea<T> : NsgaIII<T> where T : Chromosome<T>
	{
		private int _currentGeneration = 0, _max_iterations = 5000;
		private int _maxRepeat = 15;

		private float[] _Q; // quantum population
		private float[] _P; // observed classical population

		private float[,] _bounds;
		private int _chromlen, _catastrophe;

		private float[] _bestval;
		private float[,] _bestq;
		
		private int _bestNotEnhance = 0;

		// Initializes Adaptive Population NSGA-III with Dual Control Strategy
		public Rqiea(T prototype, int numberOfCrossoverPoints = 2, int mutationSize = 2, float crossoverProbability = 80, float mutationProbability = 3) : base(prototype, numberOfCrossoverPoints, mutationSize, crossoverProbability, mutationProbability)
		{
			_maxRepeat = Math.Min(_maxRepeat, _max_iterations / 2);
		}

		protected override void Initialize(List<T> population)
		{
			_chromlen = 0;
			_catastrophe = (int) _mutationProbability;
			
			List<int> bounds = new();
			for (int i = 0; i < _populationSize; ++i) {
				if(i < 1) {
					// initialize new population with chromosomes randomly built using prototype
					population.Add(_prototype.MakeEmptyFromPrototype(bounds));
					
					_chromlen = bounds.Count;
					_Q = new float[_populationSize * _chromlen * 2];
					_P = new float[_populationSize * _chromlen];
					_bounds = new float[_chromlen, 2];
					_bestval = new float[_chromlen];
					_bestq = new float[_chromlen, 2];
				}
				else
					population.Add(_prototype.MakeEmptyFromPrototype());
				
				for (int j = 0; j < _chromlen; ++j) {
					int qij = i * 2 * _chromlen + 2 * j;
					var alpha = 2.0f * (float) Configuration.Random() - 1;
					var beta = (float) (Math.Sqrt(1 - alpha * alpha) * ((Configuration.Rand(int.MaxValue) % 2 != 0) ? -1 : 1));
					_Q[qij] = alpha;
					_Q[qij + 1] = beta;
				}
			}
			
			for (int i = 0; i < bounds.Count; ++i)
				_bounds[i, 1] = bounds[i];
		}
		
		private static float[] CopyOfRange(float[] src, int start, int end) {
			int len = end - start;
			var dest = new float[len];
			Array.Copy(src, start, dest, 0, len);
			return dest;
		}

		private void Observe(List<T> population) {
			for (int i = 0; i < _populationSize; ++i) {
				for (int j = 0; j < _chromlen; ++j) {
					int pij = i * _chromlen + j;
					int qij = 2 * pij;
					
					if (Configuration.Random() <= .5)
						_P[pij] = _Q[qij] * _Q[qij];
					else
						_P[pij] = _Q[qij + 1] * _Q[qij + 1];
					
					_P[pij] *= _bounds[j, 1] - _bounds[j, 0];
					_P[pij] += _bounds[j, 0];
				}

				int start = i * _chromlen;
				if(population[i].Fitness < chromosome.Fitness || Configuration.Rand(100) <= _catastrophe) {
					var positions = CopyOfRange(_P, start, start + _chromlen);
					var chromosome = _prototype.MakeEmptyFromPrototype(null);
					chromosome.UpdatePositions(positions);
					population[i] = chromosome;
				}
				else {
					var positions = new float[_chromlen];
					population[i].ExtractPositions(positions);
					Array.Copy(positions, 0, _P, start, _chromlen);
				}
			}
		}

		private void Storebest(List<T> population) {
			int i_best = 0;
			for (int i = 1; i < _populationSize; i++) {
				if (population[i].Dominates(population[i_best]))
					i_best = i;
			}
			
			if (_best == null || population[i_best].Dominates(_best)) {
				_best = population[i_best];
				Array.Copy(_P, i_best * _chromlen, _bestval, 0, _chromlen);
				
				int start = i_best * _chromlen * 2;
				for(int i = start, j = 0; i < start + _chromlen * 2; ++j) {
					_bestq[j, 0] = _Q[i++];
					_bestq[j, 1] = _Q[i++];
				}
			}
		}
		
		private void Evaluate() {
			// not implemented
		}

		private static float Sign(double x) {
			if (x > 0)
				return 1;
			if (x < 0)
				return -1;
			return 0;
		}
		
		private static float Lut(float alpha, float beta, float alphabest, float betabest) {
			var M_PI_2 = Math.PI / 2;
			var eps = 1e-5f;
			var xi = (float) Math.Atan(beta / (alpha + eps));
			var xi_b = (float) Math.Atan(betabest / (alphabest + eps));
			if (Math.Abs(xi_b) < eps || Math.Abs(xi) < eps // xi_b or xi = 0
					|| Math.Abs(xi_b - M_PI_2) < eps || Math.Abs(xi_b - M_PI_2) < eps // xi_b or xi = pi/2
					|| Math.Abs(xi_b + M_PI_2) < eps || Math.Abs(xi_b + M_PI_2) < eps) // xi_b or xi = -pi/2
			{
				return (Configuration.Rand(int.MaxValue) % 2 != 0) ? -1 : 1;
			}

			if (xi_b > 0 && xi > 0)
				return xi_b >= xi ? 1 : -1;

			if (xi_b > 0 && xi < 0)
				return Sign(alpha * alphabest);

			if (xi_b < 0 && xi > 0)
				return -Sign(alpha * alphabest);

			if (xi_b < 0 && xi < 0)
				return xi_b >= xi ? 1 : -1;

			return Sign(xi_b);
		}
		
		private void Update() {
			for (int i = 0; i < _populationSize; ++i) {
				for (int j = 0; j < _chromlen; ++j) {
					int qij = 2 * (i * _chromlen + j);
					var qprim = new float[2];

					var k = Math.PI / (100 + _currentGeneration % 100);
					var theta = k * Lut(_Q[qij], _Q[qij + 1], _bestq[j, 0], _bestq[j, 1]);

					qprim[0] = (float) (_Q[qij] * Math.Cos(theta) + _Q[qij + 1] * (-Math.Sin(theta)));
					qprim[1] = (float) (_Q[qij] * Math.Sin(theta) + _Q[qij + 1] * (Math.Cos(theta)));

					_Q[qij] = qprim[0];
					_Q[qij + 1] = qprim[1];
				}
			}
		}
		
		private void Recombine() {
			int j;
			int i = Configuration.Rand(_populationSize);
			do {
				j = Configuration.Rand(_populationSize);
			} while (i == j);

			int h1 = Configuration.Rand(_chromlen);
			int h2 = Configuration.Rand(_chromlen - h1) + h1;

			int q1 = i * _chromlen * 2;
			int q2 = j * _chromlen * 2;

			var buf = new float[2 * _chromlen];
			Array.Copy(_Q, q1, buf, 0, 2 * _chromlen);

			Array.Copy(_Q, q2 + h1, _Q, q1 + h1 * 2, (h2 - h1) * 2);
			Array.Copy(buf, h1, _Q, q2 + h1 * 2, (h2 - h1) * 2);

			for (int k = h1; k < h2; ++k) {
				float tmp = _Q[q1 + k * 2];
				_Q[q1 + k * 2] = _Q[q2 + k * 2];
				_Q[q2 + k * 2] = tmp;
			}
		}

		// Starts and executes algorithm
		public override void Run(int maxRepeat = 9999, double minFitness = 0.999)
		{
			if (_prototype == null)
				return;

			var pop = new List<T>[2];
			pop[0] = new List<T>();
			Initialize(pop[0]);
			_currentGeneration = 0;
			Observe(pop[0]);
			Evaluate();
			Storebest(pop[0]);
		
			_bestNotEnhance = 0;
			double lastBestFit = 0.0;

			int cur = 0, next = 1;
			while(_currentGeneration < _max_iterations)
			{
				var best = Result;
				if (_currentGeneration > 0)
				{
					var status = string.Format("\rFitness: {0:F6}\t Generation: {1}    ", best.Fitness, _currentGeneration);
					if(_bestNotEnhance >= _maxRepeat)
						status = string.Format("\rFitness: {0:F6}\t Generation: {1} ...", best.Fitness, _currentGeneration);
					Console.Write(status);

					// algorithm has reached criteria?
					if (best.Fitness > minFitness)
						break;

					var difference = Math.Abs(best.Fitness - lastBestFit);
					if (difference <= 1e-6)
						++_bestNotEnhance;
					else {
						lastBestFit = best.Fitness;
						_bestNotEnhance = 0;
					}

					if (_bestNotEnhance > (maxRepeat / 100))
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
				
				if (_bestNotEnhance >= _maxRepeat && _currentGeneration % 4 == 0) {
					for (int i = 0; i < _populationSize; ++i) {
						var positions = new float[_chromlen];
						int start = i * _chromlen;
						pop[cur][i].ExtractPositions(positions);
						Array.Copy(positions, 0, _P, start, _chromlen);
					}

					Observe(pop[cur]);
					Evaluate();
					Storebest(pop[cur]);
					Update();
					Recombine();
				}

				++_currentGeneration;
			}
		}

		public override string ToString()
		{
			return "Real observation QIEA (rQIEA)";
		}
	}
}
