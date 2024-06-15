using System;
using System.Collections.Generic;
using System.Linq;
using GaSchedule.Model;

namespace GaSchedule.Algorithm
{
	internal sealed class LévyFlights<T> where T : Chromosome<T> {

		private int _chromlen;
		private double _beta, _σu, _σv;

		internal LévyFlights(int chromlen)
		{
			_chromlen = chromlen;

			_beta = 1.5;
			var num = Gamma(1 + _beta) * Math.Sin(Math.PI * _beta / 2);
			var den = Gamma((1 + _beta) / 2) * _beta * Math.Pow(2, (_beta - 1) / 2);
			_σu = Math.Pow(num / den, 1 / _beta);
			_σv = 1;
		}
		
		private static double Gamma(double z)
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
		
		internal float[] Optimum(float[] localVal, T chromosome)
		{
			var localBest = chromosome.MakeEmptyFromPrototype();
			localBest.UpdatePositions(localVal);
			
			if(localBest.Dominates(chromosome)) {
				chromosome.UpdatePositions(localVal);
				return localVal;
			}

			var positions = new float[_chromlen];
			chromosome.ExtractPositions(positions);
			return positions;
		}

		internal float[] UpdatePosition(T chromosome, float[][] currentPosition, int i, float[] gBest)
		{
			var curPos = currentPosition[i].ToArray();
			var u = Configuration.NextGaussian() * _σu;
			var v = Configuration.NextGaussian() * _σv;
			var S = u / Math.Pow(Math.Abs(v), 1 / _beta);
			
			if(gBest == null) {
				gBest = new float[_chromlen];
                chromosome.ExtractPositions(gBest);
			}
			else
				gBest = Optimum(gBest, chromosome);

			for(int j = 0; j < _chromlen; ++j)
				currentPosition[i][j] += (float) (Configuration.NextGaussian() * 0.01 * S * (curPos[j] - gBest[j]));

			currentPosition[i] = Optimum(currentPosition[i], chromosome);
			return gBest;
		}

		internal float[] UpdatePositions(List<T> population, int populationSize, float[][] currentPosition, float[] gBest)
		{
			for(int i = 0; i < populationSize; ++i)
				gBest = UpdatePosition(population[i], currentPosition, i, gBest);

			return gBest;
		}
	}
}
