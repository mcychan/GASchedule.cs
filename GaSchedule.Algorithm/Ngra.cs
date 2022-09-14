using System.Collections.Generic;
using System.Linq;

using GaSchedule.Model;

/*
 * al jadaan, Omar & Rajamani, Lakishmi & Rao, C.. (2008). 
 * Non-dominated ranked genetic algorithm for solving multi-objective optimization problems: 
 * NRGA. Journal of Theoretical and Applied Information Technology.
 * Copyright (c) 2020 - 2022 Miller Cy Chan
 */

namespace GaSchedule.Algorithm
{
	/****************** Non-dominated Ranking Genetic Algorithm (NRGA) **********************/
	public class Ngra<T> : NsgaII<T> where T : Chromosome<T>
	{
		public Ngra(T prototype, int numberOfCrossoverPoints = 2, int mutationSize = 2, float crossoverProbability = 80, float mutationProbability = 3) : base(prototype, numberOfCrossoverPoints, mutationSize, crossoverProbability, mutationProbability)
		{
		}

		/************** calculate crowding distance function ***************************/
		protected override List<T> Replacement(List<T> population)
		{
			var obj = Enumerable.Range(0, population.Count).ToDictionary(m => m, m => population[m].Fitness);
			var sortedIndices = obj.OrderByDescending(e => e.Value).Select(e => e.Key).ToArray();

			int totalFitness = (population.Count + 1) * population.Count / 2;

			var probSelection = Enumerable.Range(0, population.Count).Select(i => i * 1.0 / totalFitness).ToList();
			var cumProb = Enumerable.Range(0, population.Count).Select(i => probSelection.GetRange(0, i + 1).Sum()).ToArray();

			var selectIndices = Enumerable.Range(0, population.Count).Select(i => Configuration.Random()).ToArray();

			var parent = new T[2];
			int parentIndex = 0;
			var offspring = new List<T>();
			for (int i = 0; i < population.Count; ++i)
			{
				bool selected = false;
				for (int j = 0; j < population.Count - 1; ++j)
				{
					if (cumProb[j] < selectIndices[i] && cumProb[j + 1] >= selectIndices[i])
					{
						parent[parentIndex++ % 2] = population[sortedIndices[j + 1]];
						selected = true;
						break;
					}
				}

				if (!selected)
					parent[parentIndex++ % 2] = population[sortedIndices[i]];

				if (parentIndex % 2 == 0)
				{
					var child0 = parent[0].Crossover(parent[1], _numberOfCrossoverPoints, _crossoverProbability);
					var child1 = parent[1].Crossover(parent[0], _numberOfCrossoverPoints, _crossoverProbability);
					offspring.Add(child0);
					offspring.Add(child1);
				}
			}

			return offspring;
		}

		protected override void Initialize(List<T> population)
		{
			base.Initialize(population);
			List<T> offspring = Replacement(population);
			population.Clear();
			population.AddRange(offspring);
		}

		public override string ToString()
		{
			return "Non-dominated Ranking Genetic Algorithm (NRGA)";
		}
	}
}
