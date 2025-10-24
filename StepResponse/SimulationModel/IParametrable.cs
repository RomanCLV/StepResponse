using System.Collections.Generic;

namespace StepResponse.SimulationModel
{
    public interface IParametrable
    {
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
