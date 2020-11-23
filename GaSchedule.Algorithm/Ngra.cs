using System;
using System.Collections.Generic;
using System.Linq;

namespace GaSchedule.Algorithm
{
	/****************** Non-dominated Ranking Genetic Algorithm (NRGA) **********************/
	public class Ngra<T> : NsgaII<T> where T : Chromosome<T>
    {
		public Ngra(T prototype, int numberOfCrossoverPoints = 2, int mutationSize = 2, float crossoverProbability = 80, float mutationProbability = 3) : base(prototype, numberOfCrossoverPoints, mutationSize, crossoverProbability, mutationProbability)
		{
		}

		/************** calculate crowding distance function ***************************/
		protected override ISet<int> CalculateCrowdingDistance(ISet<int> front, List<T> totalChromosome)
		{
			float divisor = _populationSize * (_populationSize + 1);
			var distance = front.ToDictionary(m => m, _ => 0.0f);
			var obj = front.ToDictionary(m => m, m => 2 * m / divisor);

			var sortedKeys = obj.OrderBy(e => e.Value).Select(e => e.Key).ToArray();
			distance[sortedKeys[front.Count - 1]] = float.MaxValue;
			distance[sortedKeys[0]] = float.MaxValue;

			var values = new HashSet<float>(obj.Values);
			if (values.Count > 1)
			{
				for (int i = 1; i < front.Count - 1; ++i)
					distance[sortedKeys[i]] = distance[sortedKeys[i]] + (obj[sortedKeys[i + 1]] - obj[sortedKeys[i - 1]]) / (obj[sortedKeys[front.Count - 1]] - obj[sortedKeys[0]]);
			}
			return distance.OrderBy(e => e.Value).Select(e => e.Key).Reverse().ToHashSet();
		}
	}
}
