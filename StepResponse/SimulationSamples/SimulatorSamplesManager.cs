using System;
using StepResponse.SimulationSamples;
using System.Collections.Generic;
using System.Collections;

namespace StepResponse.Samples
{
    internal class SimulatorSamplesManager
    {
        private readonly List<SimulationSampleCollection> _collections;

        private uint _samplesPerCollection;
        public uint SamplesPerCollection => _samplesPerCollection;

        public int CollectionsCount => _collections.Count;

        public SimulatorSamplesManager()
        {
            _collections = new List<SimulationSampleCollection>();
        }

        public void Clear()
        {
            _collections.Clear();
            _samplesPerCollection = 0;
        }

        public void Reserve(int collectionCount, float samplingTimeSecs, float simulationTimeSecs)
        {
            Clear();
            int sampleCount = (int)(simulationTimeSecs / samplingTimeSecs) + 1;
            for (int i = 0; i < collectionCount; i++)
            {
                _collections.Add(new SimulationSampleCollection(sampleCount));
            }
        }

        public void AddSample(int collectionIndex, SimulationSample sample)
        {
            if (collectionIndex < 0 || collectionIndex >= _collections.Count)
                throw new ArgumentOutOfRangeException(nameof(collectionIndex));
            _collections[collectionIndex].Add(sample);
        }

        public void AddSample(int collectionIndex, float time, float target, float output, ulong computeTimeUs)
        {
            if (collectionIndex < 0 || collectionIndex >= _collections.Count)
                throw new ArgumentOutOfRangeException(nameof(collectionIndex));
            _collections[collectionIndex].Add(new SimulationSample(time, target, output, computeTimeUs));
        }

        public void IncreaseSamplesPerCollection()
        {
            _samplesPerCollection++;
        }

        /// <summary>
        /// Récupère un échantillon par indices (throws si hors bornes).
        /// Lecture directe, pas de copie.
        /// </summary>
        internal SimulationSample GetSample(int collectionIndex, int sampleIndex)
        {
            if (collectionIndex < 0 || collectionIndex >= _collections.Count)
                throw new ArgumentOutOfRangeException(nameof(collectionIndex));
            var col = _collections[collectionIndex];
            if (sampleIndex < 0 || sampleIndex >= col.Count)
                throw new ArgumentOutOfRangeException(nameof(sampleIndex));
            return col[sampleIndex];
        }

        /// <summary>
        /// Retourne une vue read-only (sans copie) sur la collection.
        /// </summary>
        internal IReadOnlyList<SimulationSample> GetSamples(int collectionIndex)
        {
            if (collectionIndex < 0 || collectionIndex >= _collections.Count)
                throw new ArgumentOutOfRangeException(nameof(collectionIndex));
            return new ReadOnlySimulationSampleCollectionView(_collections[collectionIndex]);
        }

        private class ReadOnlySimulationSampleCollectionView : IReadOnlyList<SimulationSample>
        {
            private readonly SimulationSampleCollection _inner;

            public ReadOnlySimulationSampleCollectionView(SimulationSampleCollection inner)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            }

            public SimulationSample this[int index] => _inner[index];

            public int Count => _inner.Count;

            public IEnumerator<SimulationSample> GetEnumerator()
            {
                for (int i = 0; i < _inner.Count; i++)
                    yield return _inner[i];
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
