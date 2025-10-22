using System;
using System.Collections.Generic;

#nullable enable

namespace StepResponse.SimulationModel
{
    internal readonly struct Parameter
    {
        public readonly string Name;
        public readonly float Value;

        public Parameter(string name, float value)
        {
            Name = name;
            Value = value;
        }
    }

    internal abstract class SimulationModel : IParametrable
    {
        public EventHandler? ParametersChanged;

        /// <summary>
        /// Reset simulation state
        /// </summary>
        internal abstract void Reset();

        internal abstract float CurrentOutput();

        internal abstract float Update(float input, float elapsedTime);

        /// <summary>
        /// Return parameters for UI/export
        /// </summary>
        public abstract Dictionary<string, float> GetParameters();

        /// <summary>
        /// Get parameter by name.
        /// </summary>
        public abstract bool GetParameter(string param, out float value);

        /// <summary>
        /// Set parameter by name.
        /// </summary>
        public abstract bool SetParameter(string param, float value);

        public abstract bool IsValidValue(string param, float value);
    }
}
