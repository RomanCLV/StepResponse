using System.Collections.Generic;

namespace StepResponse.SimulationModel
{
    public interface IParametrable
    {
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
