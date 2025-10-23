using System;
using System.Collections.Generic;

namespace StepResponse.SimulationModel
{
    /// <summary>
    /// First-order system:
    ///   T * y'(t) + y(t) = K * u(t)
    /// Discretized with Forward Euler:
    ///   y[n] = y[n-1] + (dt / T) * (K * u[n] - y[n-1])
    /// </summary>
    internal class FirstOrderModel : SimulationModel
    {
        // Keys as const strings for GetParameter/SetParameter
        public const string K_KEY = "K";
        public const string T_KEY = "T";

        // Model parameters as fields
        private float _k;
        private float _t;

        // Simulation state (previous outputs for second-order system)
        private float _previousOutput; // y[n-1]

        // Properties
        public float K 
        {
            get => _k;
            set 
            {
                if (_k != value)
                {
                    _k = value;
                    ParametersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public float T 
        {
            get => _t;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(T), "T must be positive.");

                if (_t != value)
                {
                    _t = value;
                    ParametersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public FirstOrderModel() : this(1f, 1f) { }

        public FirstOrderModel(float k, float t)
        {
            _k = k;
            _t = t;
            _previousOutput = 0f;
        }

        internal override void Reset()
        {
            _previousOutput = 0f;
        }

        internal override void SetCurrent(float current)
        {
            _previousOutput = current;
        }

        internal override float CurrentOutput()
        {
            return _previousOutput;
        }

        /// <summary>
        /// First-order system discretization using Forward Euler.
        /// </summary>
        /// <param name="input">Current input value (u[n])</param>
        /// <param name="elapsedTime">Sampling interval (dt)</param>
        /// <returns>Current output value (y[n])</returns>
        internal override float Update(float input, float elapsedTime)
        {
            // y[n] = y[n-1] + (dt / T) * (K * u[n] - y[n-1])

            _previousOutput = _previousOutput + (elapsedTime / _t) * (_k * input - _previousOutput);
            return _previousOutput;
        }

        public override Dictionary<string, float> GetParameters()
        {
            return new Dictionary<string, float>
            {
                { K_KEY, _k },
                { T_KEY, _t }
            };
        }

        public override bool GetParameter(string param, out float value)
        {
            switch (param)
            {
                case K_KEY: value = _k; return true;
                case T_KEY: value = _t; return true;
                default: value = 0f; return false;
            }
        }

        public override bool SetParameter(string param, float value)
        {
            switch (param)
            {
                case K_KEY: K = value; return true;
                case T_KEY: T = value; return true;
                default: return false;
            }
        }

        public override bool IsValidValue(string param, float value)
        {
            switch (param)
            {
                case K_KEY: return true; // K can be any float
                case T_KEY: return value > 0; // T must be positive
                default: return false;
            }
        }
    }
}
