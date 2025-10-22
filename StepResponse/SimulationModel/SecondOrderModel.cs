using System;
using System.Collections.Generic;

namespace StepResponse.SimulationModel
{
    /// <summary>
    /// Second-order system in standard form:
    ///   y'' + 2*Z*W0*y' + W0^2*y = K * W0^2 * u
    ///
    /// Discretized using Tustin / bilinear transform leading to:
    ///   y[n] = -a1*y[n-1] - a2*y[n-2] + b0*u[n]
    ///
    /// Coefficients are computed at each step using dt (supports variable dt).
    /// </summary>
    internal class SecondOrderModel : SimulationModel
    {
        // Keys as const strings for GetParameter/SetParameter
        public const string K_KEY = "K";
        public const string W0_KEY = "W0";
        public const string Z_KEY = "Z";

        // Model parameters as fields
        private float _k;
        private float _w0;
        private float _z;

        // Simulation state (previous outputs for second-order system)
        private float _previousOutput1; // y[n-1]
        private float _previousOutput2; // y[n-2]

        // Properties for direct access
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

        public float W0 
        {
            get => _w0;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(W0), "W0 must be positive.");

                if (_w0 != value)
                {
                    _w0 = value;
                    ParametersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public float Z 
        {
            get => _z;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Z), "Z must be non-negative.");

                if (_z != value)
                {
                    _z = value;
                    ParametersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        // Constructors
        public SecondOrderModel() : this(1f, 1f, 0.5f) { }

        public SecondOrderModel(float k, float w0, float z)
        {
            _k = k;
            _w0 = w0;
            _z = z;

            _previousOutput1 = 0f;
            _previousOutput2 = 0f;
        }

        internal override float CurrentOutput()
        {
            return _previousOutput1;
        }

        internal override void Reset()
        {
            _previousOutput1 = 0f;
            _previousOutput2 = 0f;
        }

        /// <summary>
        /// Second-order system discretization (Tustin / bilinear approximation).
        /// </summary>
        /// <param name="input">Current input value (u[n])</param>
        /// <param name="elapsedTime">Sampling interval (dt)</param>
        /// <returns>Current output value (y[n])</returns>
        internal override float Update(float input, float elapsedTime)
        {
            // Continuous-time: y'' + 2*Z*W0*y' + W0^2*y = K*W0^2*u
            // Discrete-time difference equation: y[n] = a1*y[n-1] + a2*y[n-2] + b0*u[n]

            // Coefficients for bilinear approximation
           
            // _w0: natural frequency
            // _z : damping ratio
            // _k : system gain

            float t2 = elapsedTime * elapsedTime;
            float w2 = _w0 * _w0;
            float w2t2 = w2 * t2;
            float tmp = 4f * _z * _w0 * elapsedTime + w2t2;

            float a0 = 4f + tmp;
            float a1 = (-8f + 2f * w2t2) / a0;
            float a2 = (4f - tmp) / a0;
            float b0 = _k * w2t2 / a0;

            float output = -a1 * _previousOutput1 - a2 * _previousOutput2 + b0 * input;

            // Update state
            _previousOutput2 = _previousOutput1;
            _previousOutput1 = output;

            return output;
        }

        public override Dictionary<string, float> GetParameters()
        {
            return new Dictionary<string, float>
            {
                { K_KEY, _k },
                { W0_KEY, _w0 },
                { Z_KEY, _z }
            };
        }

        public override bool GetParameter(string param, out float value)
        {
            switch (param)
            {
                case K_KEY: value = _k; return true;
                case W0_KEY: value = _w0; return true;
                case Z_KEY: value = _z; return true;
                default: value = 0f; return false;
            }
        }

        public override bool SetParameter(string param, float value)
        {
            switch (param)
            {
                case K_KEY: K = value; return true;
                case W0_KEY: W0 = value; return true;
                case Z_KEY: Z = value; return true;
                default: return false;
            }
        }

        public override bool IsValidValue(string param, float value)
        {
            switch (param)
            {
                case K_KEY:
                    return true; // K can be any float
                case W0_KEY:
                    return value > 0f; // W0 must be positive
                case Z_KEY:
                    return value >= 0f; // Z must be non-negative
                default:
                    return false; // Unknown parameter
            }
        }
    }
}
