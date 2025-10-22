using StepResponse.Samples;
using System.Collections.Generic;

namespace StepResponse.SimulationSamples
{
    internal interface IReadOnlySimulatorSamplesManager
    {
        uint SamplesPerCollection { get; }
        int CollectionsCount { get; }
        SimulationSample GetSample(int collectionIndex, int sampleIndex);
        IReadOnlyList<SimulationSample> GetSamples(int collectionIndex);
    }
}
