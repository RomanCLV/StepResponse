using System;
using System.Collections.Generic;

#nullable enable

namespace StepResponse.SimulationModel
{
    internal readonly struct Parameter
    {
        public readonly string Name;
        public readonly double Value;

        public Parameter(string name, double value)
        {
            Name = name;
            Value = value;
        }
    }

    internal abstract class SimulationModel : IParametrable
    {
        public EventHandler? ParametersChanged;

        public virtual string Name => GetType().Name;

        /// <summary>
        /// Reset simulation state
        /// </summary>
        internal abstract void Reset();

        internal abstract double CurrentOutput();

        internal abstract void SetCurrent(double current);

        internal abstract double Update(double input, double elapsedTime);

        /// <summary>
        /// Return parameters for UI/export
        /// </summary>
        public abstract Dictionary<string, double> GetParameters();

        /// <summary>
        /// Get parameter by name.
        /// </summary>
        public abstract bool GetParameter(string param, out double value);

        /// <summary>
        /// Set parameter by name.
        /// </summary>
        public abstract bool SetParameter(string param, double value);

        public abstract bool IsValidValue(string param, double value);
    }
}
