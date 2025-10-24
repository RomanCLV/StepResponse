namespace StepResponse.Samples
{
    internal readonly struct SimulationSample
    {
        public readonly double Time;
        public readonly double Output;
        public readonly double Target;
        public readonly ulong ComputeTimeUs;

        public SimulationSample(double time, double target, double output, ulong computeTimeUs)
        {
            Time = time;
            Target = target;
            Output = output;
            ComputeTimeUs = computeTimeUs;
        }
    }
}
