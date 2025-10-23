using System;
using System.Collections.Generic;

namespace StepResponse.SimulationModel
{
    internal class LinearModel : SimulationModel
    {
        // Keys as const strings for GetParameter/SetParameter
        public const string K_KEY = "K";
        public const string R_KEY = "R";

        private float _k;
        private float _r;

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

        public float R
        {
            get => _r;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(R), "R must be positive.");

                if (_r != value)
                {
                    _r = value;
                    ParametersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public LinearModel() : this(1f, 1f) { }

        public LinearModel(float k, float r)
        {
            _k = k;
            _r = r;
            _previousOutput = 0f;
        }

        internal override void Reset()
        {
            _previousOutput = 0f;
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
            float target = _k * input;
            if (_previousOutput > target)
            {
                _previousOutput = Math.Max(target, _previousOutput - _r * elapsedTime);
            }
            else if (_previousOutput < target)
            {
                _previousOutput = Math.Min(target, _previousOutput + _r * elapsedTime);
            }

            return _previousOutput;
        }

        public override Dictionary<string, float> GetParameters()
        {
            return new Dictionary<string, float>
            {
                { K_KEY, _k },
                { R_KEY, _r }
            };
        }

        public override bool GetParameter(string param, out float value)
        {
            switch (param)
            {
                case K_KEY: value = _k; return true;
                case R_KEY: value = _r; return true;
                default: value = 0f; return false;
            }
        }

        public override bool SetParameter(string param, float value)
        {
            switch (param)
            {
                case K_KEY: K = value; return true;
                case R_KEY: R = value; return true;
                default: return false;
            }
        }

        public override bool IsValidValue(string param, float value)
        {
            switch (param)
            {
                case K_KEY: return true; // K can be any float
                case R_KEY: return value > 0; // R must be positive
                default: return false;
            }
        }
    }
}
