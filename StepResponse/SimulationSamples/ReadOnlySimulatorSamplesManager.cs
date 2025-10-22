using System;
using System.Collections.Generic;
using StepResponse.SimulationSamples;

namespace StepResponse.Samples
{
    internal class ReadOnlySimulatorSamplesManager : IReadOnlySimulatorSamplesManager
    {
        private readonly SimulatorSamplesManager _inner;

        public ReadOnlySimulatorSamplesManager(SimulatorSamplesManager inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public uint SamplesPerCollection => _inner.SamplesPerCollection;

        public int CollectionsCount => _inner.CollectionsCount;

        public SimulationSample GetSample(int collectionIndex, int sampleIndex) => _inner.GetSample(collectionIndex, sampleIndex);

        public IReadOnlyList<SimulationSample> GetSamples(int collectionIndex) => _inner.GetSamples(collectionIndex);
    }
}
