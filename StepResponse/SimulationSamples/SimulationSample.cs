namespace StepResponse.Samples
{
    internal readonly struct SimulationSample
    {
        public readonly float Time;
        public readonly float Output;
        public readonly float Target;
        public readonly ulong ComputeTimeUs;

        public SimulationSample(float time, float target, float output, ulong computeTimeUs)
        {
            Time = time;
            Target = target;
            Output = output;
            ComputeTimeUs = computeTimeUs;
        }
    }
}
