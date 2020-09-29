namespace GaSchedule.Algorithm
{
    public interface Chromosome<T> where T : Chromosome<T>
    {
        public T MakeNewFromPrototype();

        public float Fitness { get; }

        public Configuration Configuration { get; }

        public T Crossover(T mother, int numberOfCrossoverPoints, float crossoverProbability);

        public void Mutation(int mutationSize, float mutationProbability);
    }
}
