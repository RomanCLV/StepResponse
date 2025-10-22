using StepResponse.Samples;
using System;
using System.Collections;
using System.Collections.Generic;

namespace StepResponse.SimulationSamples
{
    internal class SimulationSampleCollection : IList<SimulationSample>
    {
        private readonly List<SimulationSample> _samples;

        public SimulationSampleCollection()
        {
            _samples = new List<SimulationSample>();
        }

        public SimulationSampleCollection(int capacity)
        {
            _samples = new List<SimulationSample>(capacity);
        }

        public SimulationSampleCollection(IEnumerable<SimulationSample> items)
        {
            if (items == null) 
                throw new ArgumentNullException(nameof(items));
            _samples = new List<SimulationSample>(items);
        }

        // --- IList implementation ---
        public SimulationSample this[int index]
        {
            get => _samples[index];
            set => _samples[index] = value;
        }

        public int Count => _samples.Count;

        public bool IsReadOnly => false;

        public void Add(SimulationSample item) => _samples.Add(item);

        public void Clear() => _samples.Clear();

        public bool Contains(SimulationSample item) => _samples.Contains(item);

        public void CopyTo(SimulationSample[] array, int arrayIndex) => _samples.CopyTo(array, arrayIndex);

        public IEnumerator<SimulationSample> GetEnumerator() => _samples.GetEnumerator();

        public int IndexOf(SimulationSample item) => _samples.IndexOf(item);

        public void Insert(int index, SimulationSample item) => _samples.Insert(index, item);

        public bool Remove(SimulationSample item) => _samples.Remove(item);

        public void RemoveAt(int index) => _samples.RemoveAt(index);

        IEnumerator IEnumerable.GetEnumerator() => _samples.GetEnumerator();

        // --- Extras pratiques ---
        public void AddRange(IEnumerable<SimulationSample> items) => _samples.AddRange(items);

        public void TrimExcess() => _samples.TrimExcess();

        public SimulationSample[] ToArray() => _samples.ToArray();
    }
}
