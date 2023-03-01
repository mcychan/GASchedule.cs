using System;
using System.Collections.Generic;
using System.Linq;
using GaSchedule.Model;

/*
 * Wu, M.; Yang, D.; Zhou, B.; Yang, Z.; Liu, T.; Li, L.; Wang, Z.; Hu,
 * K. Adaptive Population NSGA-III with Dual Control Strategy for Flexible Job
 * Shop Scheduling Problem with the Consideration of Energy Consumption and Weight. Machines 2021, 9, 344.
 * https://doi.org/10.3390/machines9120344
 * Copyright (c) 2023 Miller Cy Chan
 */

namespace GaSchedule.Algorithm
{
	/****************** Evolutionary multi-objective seagull optimization algorithm (EMoSOA) **********************/
	public class APNsgaIII<T> : NsgaIII<T> where T : Chromosome<T>
	{
		// Worst of chromosomes
		protected T _worst;

		// Initializes Adaptive Population NSGA-III with Dual Control Strategy
		public APNsgaIII(T prototype, int numberOfCrossoverPoints = 2, int mutationSize = 2, float crossoverProbability = 80, float mutationProbability = 3) : base(prototype, numberOfCrossoverPoints, mutationSize, crossoverProbability, mutationProbability)
		{
		}

		private double Ex(T chromosome)
		{
			double numerator = 0.0, denominator = 0.0;
			for (int f = 0; f < chromosome.Objectives.Length; ++f) {
				numerator += chromosome.Objectives[f] - _best.Objectives[f];
				denominator += _worst.Objectives[f] - _best.Objectives[f];
			}
			return (numerator + 1) / (denominator + 1);
		}
		
		private void PopDec(List<T> population)
		{
			var N = population.Count;
			if(N <= _populationSize)
				return;
			

			var rank = (int) (.3 * _populationSize);
			
			for(int i = 0; i < N; ++i) {
				var exValue = Ex(population[i]);
				
				if(exValue > .5 && i > rank) {
					population.RemoveAt(i);				
					if(--N <= _populationSize)
						break;
				}
			}
		}

		private void DualCtrlStrategy(List<T> population, int bestNotEnhance, int nMax)
		{
			int N = population.Count;
			int nTmp = N;
			for(int i = 0; i < nTmp; ++i) {
				var chromosome = population[i];
				var tumor = chromosome.Clone();
				tumor.Mutation(_mutationSize, _mutationProbability);
				
				_worst = population[population.Count - 1];
				if(Dominate(tumor, chromosome)) {
					population[i] = tumor;
					if(Dominate(tumor, _best))
						_best = tumor;
				}
				else {
					if(bestNotEnhance >= 15 && N < nMax) {
						++N;
						if(Dominate(_worst, tumor)) {
							population.Add(tumor);
							_worst = tumor;
						}
						else
							population.Insert(population.size() - 1, tumor);
					}
				}				
			}
			PopDec(population);
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
					var status = string.Format("\rFitness: {0:F6}\t Generation: {1}    ", best.Fitness, currentGeneration);
					if(bestNotEnhance >= 15)
						status = string.Format("\rFitness: {0:F6}\t Generation: {1} ...", best.Fitness, currentGeneration);
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

					_repeatRatio = bestNotEnhance * 100.0f / maxRepeat;
					if (bestNotEnhance > (maxRepeat / 100))
						Reform();

				}

				/******************* crossover *****************/
				var offspring = Replacement(pop[cur]);

				/******************* mutation *****************/
				foreach (var child in offspring)
					child.Mutation(_mutationSize, _mutationProbability);

				pop[cur].AddRange(offspring);

				/******************* selection *****************/				
				pop[next] = Selection(pop[cur]);
				_best = Dominate(pop[next][0], pop[cur][0]) ? pop[next][0] : pop[cur][0];

				DualCtrlStrategy(pop[next], bestNotEnhance, nMax);
				
				(cur, next) = (next, cur);
				++currentGeneration;
			}
		}

		public override string ToString()
		{
			return "Adaptive Population NSGA-III with Dual Control Strategy (APNsgaIII)";
		}
	}
}
