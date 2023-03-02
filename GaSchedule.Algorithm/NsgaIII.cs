using System;
using System.Collections.Generic;
using System.Linq;

using GaSchedule.Model;

/*
 * Deb K , Jain H . An Evolutionary Many-Objective Optimization Algorithm Using Reference Point-Based Nondominated Sorting Approach,
 * Part I: Solving Problems With Box Constraints[J]. IEEE Transactions on Evolutionary Computation, 2014, 18(4):577-601.
 * Copyright (c) 2023 Miller Cy Chan
 */

namespace GaSchedule.Algorithm
{
	// NSGA III
	public class NsgaIII<T> where T : Chromosome<T>
	{
		// Best of chromosomes
		protected T _best;

		// Prototype of chromosomes in population
		protected T _prototype;

		// Number of chromosomes
		protected int _populationSize;

		// Number of crossover points of parent's class tables
		protected int _numberOfCrossoverPoints;

		// Number of classes that is moved randomly by single mutation operation
		protected int _mutationSize;

		// Probability that crossover will occur
		protected float _crossoverProbability;

		// Probability that mutation will occur
		protected float _mutationProbability;

		protected float _repeatRatio;
		
		private List<int> _objDivision;

		// Initializes NsgaIII
		private NsgaIII(T prototype, int numberOfChromosomes)
		{
			_prototype = prototype;
			// there should be at least 2 chromosomes in population
			if (numberOfChromosomes < 2)
				numberOfChromosomes = 2;
			_populationSize = numberOfChromosomes;
		}

		public NsgaIII(T prototype, int numberOfCrossoverPoints = 2, int mutationSize = 2, float crossoverProbability = 80, float mutationProbability = 3) : this(prototype, 100)
		{
			_mutationSize = mutationSize;
			_numberOfCrossoverPoints = numberOfCrossoverPoints;
			_crossoverProbability = crossoverProbability;
			_mutationProbability = mutationProbability;
			
			_objDivision = new List<int>();
			if(Criteria.Weights.Length < 8)
				_objDivision.Add(6);
			else {
				_objDivision.Add(3);
				_objDivision.Add(2);
			}
		}

		// Returns pointer to best chromosomes in population
		public T Result => _best;

		protected sealed class ReferencePoint {
			public int MemberSize { get; private set; }
			public double[] Position { get; private set; }

			private readonly Dictionary<int, double> potentialMembers;
			
			ReferencePoint(int M) {
				MemberSize = 0;
				Position = new double[M];
				potentialMembers = new Dictionary<int, double>();
			}
			
			static void GenerateRecursive(List<ReferencePoint> rps, ReferencePoint pt, int numObjs, int left, int total, int element) {
				if (element == numObjs - 1) {
					pt.Position[element] = left * 1.0 / total;
					rps.Add(pt);
				}
				else {
					for (int i = 0; i <= left; ++i) {
						pt.Position[element] = i * 1.0 / total;
						GenerateRecursive(rps, pt, numObjs, left - i, total, element + 1);
					}
				}
			}
			
			public void AddMember()
			{
				++MemberSize;
			}

			public void AddPotentialMember(int memberInd, double distance)
			{
				if(potentialMembers.TryGetValue(memberInd, out var currDistance))
				{
					if (distance >= currDistance)
						return;
				}
				potentialMembers[memberInd] = distance;
			}
			
			public int FindClosestMember()
			{
				var minDist = Double.MaxValue;
				int minIndv = -1;
				foreach (var entry in potentialMembers) {
					if (entry.Value < minDist) {
						minDist = entry.Value;
						minIndv = entry.Key;
					}
				}

				return minIndv;
			}
			
			public bool HasPotentialMember()
			{
				return potentialMembers.Any();
			}

			public int RandomMember()
			{
				if (!potentialMembers.Any())
					return -1;

				var members = potentialMembers.Keys.ToArray();
				return members[Configuration.Rand(potentialMembers.Count)];			
			}
			
			public void RemovePotentialMember(int memberInd)
			{
				potentialMembers.Remove(memberInd);
			}

			public static void GenerateReferencePoints(List<ReferencePoint> rps, int M, List<int> p) {
				var pt = new ReferencePoint(M);
				GenerateRecursive(rps, pt, M, p[0], p[0], 0);

				if (p.Count > 1) { // two layers of reference points (Check Fig. 4 in NSGA-III paper)
					var insideRps = new List<ReferencePoint>();
					GenerateRecursive(insideRps, pt, M, p[1], p[1], 0);

					var center = 1.0 / M;

					foreach (var insideRp in insideRps) {
						for (int j = 0; j < insideRp.Position.Length; ++j)
							insideRp.Position[j] = center + insideRp.Position[j] / 2; // (k=num_divisions/M, k, k, ..., k) is the center point

						rps.Add(insideRp);
					}
				}
			}
			
		}

		private static double PerpendicularDistance(double[] direction, double[] point)
		{
			double numerator = 0, denominator = 0;
			for (int i = 0; i < direction.Length; ++i) {
				numerator += direction[i] * point[i];
				denominator += Math.Pow(direction[i], 2);
			}
			
			if(denominator <= 0)
				return Double.MaxValue;
			
			var k = numerator / denominator;
			var d = 0.0;
			for (int i = 0; i < direction.Length; ++i)
				d += Math.Pow(k * direction[i] - point[i], 2);

			return Math.Sqrt(d);
		}
	
		private static void Associate(List<ReferencePoint> rps, List<T> pop, List<List<int> > fronts) {
			for (int t = 0; t < fronts.Count; ++t) {
				foreach (var memberInd in fronts[t]) {
					var minRp = rps.Count - 1;
					var minDist = Double.MaxValue;
					for (int r = 0; r < rps.Count; ++r) {
						var d = PerpendicularDistance(rps[r].Position, pop[memberInd].ConvertedObjectives);
						if (d < minDist) {
							minDist = d;
							minRp = r;
						}
					}

					if (t + 1 != fronts.Count) // associating members in St/Fl (only counting)
						rps[minRp].AddMember();
					else
						rps[minRp].AddPotentialMember(memberInd, minDist);

				}// for - members in front
			}// for - fronts
		}
	

		private static double[] GuassianElimination(List<double>[] A, double[] b)
		{
			int N = A.Length;
			for (int i = 0; i < N; ++i)
				A[i].Add(b[i]);

			for (int base_ = 0; base_ < N - 1; ++base_) {
				for (int target = base_ + 1; target < N; ++target) {
					var ratio = A[target][base_] / A[base_][base_];
					for (int term = 0; term < A[base_].Count; ++term)
						A[target][term] -= A[base_][term] * ratio;
				}
			}

			var x = new double[N];
			for (int i = N - 1; i >= 0; --i) {
				for (int known = i + 1; known < N; ++known)
					A[i][N] -= A[i][known] * x[known];

				x[i] = A[i][N] / A[i][i];
			}
			return x;
		}
	
		// ----------------------------------------------------------------------
		// ASF: Achivement Scalarization Function
		// ----------------------------------------------------------------------
		private static double ASF(double[] objs, double[] weight)
		{
			var max_ratio = -Double.MaxValue;
			for (int f = 0; f < objs.Length; ++f) {
				var w = Math.Max(weight[f], 1e-6);
				max_ratio = Math.Max(max_ratio, objs[f] / w);
			}
			return max_ratio;
		}
	

		private static List<int> FindExtremePoints(List<T> pop, List<List<int> > fronts) {
			int numObj = pop[0].Objectives.Length;
			
			var exp = new List<int>();
			for (int f = 0; f < numObj; ++f) {
				var w = Enumerable.Repeat(1e-6, numObj).ToArray();
				w[f] = 1.0;

				var minASF = Double.MaxValue;
				int minIndv = fronts[0].Count;

				foreach (var frontIndv in fronts[0]) { // only consider the individuals in the first front
					var asf = ASF(pop[frontIndv].ConvertedObjectives, w);

					if (asf < minASF) {
						minASF = asf;
						minIndv = frontIndv;
					}
				}

				exp.Add(minIndv);
			}

			return exp;
		}
	

		private static double[] FindMaxObjectives(List<T> pop)
		{
			int numObj = pop[0].Objectives.Length;
			var maxPoint = Enumerable.Repeat(-Double.MaxValue, numObj).ToArray();
			for (int i = 0; i < pop.Count; ++i) {
				for (int f = 0; f < maxPoint.Length; ++f)
					maxPoint[f] = Math.Max(maxPoint[f], pop[i].Objectives[f]);
			}

			return maxPoint;
		}
	

		private static int FindNicheReferencePoint(List<ReferencePoint> rps)
		{
			// find the minimal cluster size
			var minSize = int.MaxValue;
			foreach (var rp in rps)
				minSize = Math.Min(minSize, rp.MemberSize);

			// find the reference points with the minimal cluster size Jmin
			var minRps = new List<int>();
			for (int r = 0; r < rps.Count; ++r) {
				if (rps[r].MemberSize == minSize)
					minRps.Add(r);
			}

			// return a random reference point (j-bar)
			return minRps[Configuration.Rand(minRps.Count)];
		}
	

		private List<double> ConstructHyperplane(List<T> pop, List<int> extremePoints)
		{
			var numObj = pop[0].Objectives.Length;
			// Check whether there are duplicate extreme points.
			// This might happen but the original paper does not mention how to deal with it.
			var duplicate = false;
			for (int i = 0; !duplicate && i < extremePoints.Count; ++i) {
				for (int j = i + 1; !duplicate && j < extremePoints.Count; ++j)
					duplicate = (extremePoints[i] == extremePoints[j]);
			}

			var intercepts = new List<double>();

			var negativeIntercept = false;
			if (!duplicate) {
				// Find the equation of the hyperplane
				var b = Enumerable.Repeat(1.0, numObj).ToArray();
				var A = new List<double>[extremePoints.Count];
				for (int p = 0; p < extremePoints.Count; ++p)
					A[p] = pop[ extremePoints[p] ].ConvertedObjectives.ToList();
				
				var x = GuassianElimination(A, b);
				// Find intercepts
				for (int f = 0; f < numObj; ++f) {
					intercepts.Add(1.0 / x[f]);

					if(x[f] < 0) {
						negativeIntercept = true;
						break;
					}
				}
			}

			if (duplicate || negativeIntercept) // follow the method in Yuan et al. (GECCO 2015)
				intercepts = FindMaxObjectives(pop).ToList();
			
			return intercepts;
		}
	

		private static void NormalizeObjectives(List<T> pop, List<List<int> > fronts, List<double> intercepts, List<double> idealPoint)
		{		
			foreach (var front in fronts) {
				foreach (int ind in front) {
					var convObjs = pop[ind].ConvertedObjectives;
					for (int f = 0; f < convObjs.Length; ++f) {
						if (Math.Abs(intercepts[f] - idealPoint[f]) > 10e-10) // avoid the divide-by-zero error
							convObjs[f] /= intercepts[f] - idealPoint[f];
						else
							convObjs[f] /= 10e-10;
					}
				}
			}
		}
	
		protected bool Dominate(T left, T right) {
			var better = false;
			for (int f = 0; f < left.Objectives.Length; ++f) {
				if (left.Objectives[f] > right.Objectives[f])
					return false;
				
				if (left.Objectives[f] < right.Objectives[f])
					better = true;
			}
			return better;
		}
	
		protected List<List<int> > NondominatedSort(List<T> pop) {
			var fronts = new List<List<int> >();
			int numAssignedIndividuals = 0;
			int rank = 1;
			var indvRanks = new int[pop.Count];

			while (numAssignedIndividuals < pop.Count) {
				var curFront = new List<int>();

				for (int i = 0; i < pop.Count; ++i) {
					if (indvRanks[i] > 0)
						continue; // already assigned a rank

					var beDominated = false;
					for (int j = 0; j < curFront.Count; ++j) {
						if (Dominate(pop[ curFront[j] ], pop[i]) ) { // i is dominated
							beDominated = true;
							break;
						}
						else if ( Dominate(pop[i], pop[ curFront[j] ]) ) // i dominates a member in the current front
							curFront.RemoveAt(j--);
					}
					
					if (!beDominated)
						curFront.Add(i);
				}

				foreach (var front in curFront)
					indvRanks[front] = rank;

				fronts.Add(curFront);
				numAssignedIndividuals += curFront.Count;
				
				++rank;
			}

			return fronts;
		}
	

		private static int SelectClusterMember(ReferencePoint rp) {
			if (rp.HasPotentialMember()) {
				if (rp.MemberSize == 0) // currently has no member
					return rp.FindClosestMember();

				return rp.RandomMember();
			}
			return -1;
		}
	

		private static List<double> TranslateObjectives(List<T> pop, List<List<int> > fronts)
		{
			var idealPoint = new List<double>();
			var numObj = pop[0].Objectives.Length;
			for (int f = 0; f < numObj; ++f) {
				var minf = Double.MaxValue;
				foreach (var frontIndv in fronts[0]) // min values must appear in the first front
					minf = Math.Min(minf, pop[frontIndv].Objectives[f]);
					
				idealPoint.Add(minf);

				foreach (var front in fronts) {
					foreach (var ind in front) {				
						pop[ind].ResizeConvertedObjectives(numObj);
						var convertedObjectives = pop[ind].ConvertedObjectives;
						convertedObjectives[f] = pop[ind].Objectives[f] - minf;
					}
				}
			}
			
			return idealPoint;
		}

		protected List<T> Selection(List<T> cur, List<ReferencePoint> rps) {
			var next = new List<T>();
			
			// ---------- Step 4 in Algorithm 1: non-dominated sorting ----------
			var fronts = NondominatedSort(cur);
			
			// ---------- Steps 5-7 in Algorithm 1 ----------
			int last = 0, next_size = 0;
			while (next_size < _populationSize) {
				next_size += fronts[last++].Count;
			}
			
			fronts = fronts.GetRange(0, last); // remove useless individuals

			for (int t = 0; t < fronts.Count - 1; ++t) {
				foreach (var frontIndv in fronts[t])
					next.Add(cur[frontIndv]);
			}

			// ---------- Steps 9-10 in Algorithm 1 ----------
			if (next.Count == _populationSize)
				return next.OrderByDescending(chromosome => chromosome.Fitness).ToList();

			// ---------- Step 14 / Algorithm 2 ----------
			var idealPoint = TranslateObjectives(cur, fronts);
			
			var extremePoints = FindExtremePoints(cur, fronts);

			var intercepts = ConstructHyperplane(cur, extremePoints);

			NormalizeObjectives(cur, fronts, intercepts, idealPoint);

			// ---------- Step 15 / Algorithm 3, Step 16 ----------
			Associate(rps, cur, fronts);

			// ---------- Step 17 / Algorithm 4 ----------
			while (next.Count < _populationSize) {
				var minRp = FindNicheReferencePoint(rps);

				var chosen = SelectClusterMember(rps[minRp]);
				if (chosen < 0) // no potential member in Fl, disregard this reference point
					rps.RemoveAt(minRp);
				else {
					rps[minRp].AddMember();
					rps[minRp].RemovePotentialMember(chosen);
					next.Add(cur[chosen]);
				}
			}

			return next.OrderByDescending(chromosome => chromosome.Fitness).ToList();
		}

		protected virtual List<T> Crossing(List<T> population)
		{
			var offspring = new List<T>();
			for (int i = 0; i < _populationSize; i += 2) {
				int father = Configuration.Rand(_populationSize), mother = Configuration.Rand(_populationSize);
				var child0 = population[father].Crossover(population[mother], _numberOfCrossoverPoints, _crossoverProbability);
				var child1 = population[mother].Crossover(population[father], _numberOfCrossoverPoints, _crossoverProbability);
				offspring.Add(child0);
				offspring.Add(child1);
			}
			return offspring;
		}
		
		protected virtual void Initialize(List<T> population)
		{
			// initialize new population with chromosomes randomly built using prototype
			for (int i = 0; i < _populationSize; ++i)
				population.Add(_prototype.MakeNewFromPrototype());
		}

		protected void Reform()
		{
			Configuration.Seed();
			if (_crossoverProbability < 95)
			_crossoverProbability += 1.0f;
			else if (_mutationProbability < 30)
			_mutationProbability += 1.0f;
		}
		
		protected List<T> Replacement(List<T> population)
		{
			var rps = new List<ReferencePoint>();			
			ReferencePoint.GenerateReferencePoints(rps, Criteria.Weights.Length, _objDivision);				
			return Selection(population, rps);
		}

		// Starts and executes algorithm
		public virtual void Run(int maxRepeat = 9999, double minFitness = 0.999)
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
			for (; ; )
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

					_repeatRatio = bestNotEnhance * 100.0f / maxRepeat;
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
				_best = Dominate(pop[next][0], pop[cur][0]) ? pop[next][0] : pop[cur][0];

				
				(cur, next) = (next, cur);
				++currentGeneration;
			}
		}

		public override string ToString()
		{
			return "NSGA III";
		}
	}
}

