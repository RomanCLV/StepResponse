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
        private double _k;
        private double _w0;
        private double _z;

        // Simulation state
        private double _previousOutput1; // y[n-1]
        private double _previousOutput2; // y[n-2]

        // Properties for direct access
        public double K 
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

        public double W0 
        {
            get => _w0;
            set
            {
                if (value <= 0.0)
                    throw new ArgumentOutOfRangeException(nameof(W0), "W0 must be positive.");

                if (_w0 != value)
                {
                    _w0 = value;
                    ParametersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public double Z 
        {
            get => _z;
            set
            {
                if (value < 0.0)
                    throw new ArgumentOutOfRangeException(nameof(Z), "Z must be non-negative.");

                if (_z != value)
                {
                    _z = value;
                    ParametersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        // Constructors
        public SecondOrderModel() : this(1.0, 1.0, 0.5) { }

        public SecondOrderModel(double k, double w0, double z)
        {
            _k = k;
            _w0 = w0;
            _z = z;

            _previousOutput1 = 0.0;
            _previousOutput2 = 0.0;
        }

        internal override double CurrentOutput()
        {
            return _previousOutput1;
        }

        internal override void Reset()
        {
            _previousOutput1 = 0.0;
            _previousOutput2 = 0.0;
        }


        internal override void SetCurrent(double current)
        {
            _previousOutput1 = current;
            _previousOutput2 = current;
        }

        /// <summary>
        /// Second-order system discretization (Tustin / bilinear approximation).
        /// </summary>
        /// <param name="input">Current input value (u[n])</param>
        /// <param name="elapsedTime">Sampling interval (dt)</param>
        /// <returns>Current output value (y[n])</returns>
        internal override double Update(double input, double elapsedTime)
        {
            // Continuous-time: y'' + 2*Z*W0*y' + W0^2*y = K*W0^2*u
            // Discrete-time difference equation: y[n] = a1*y[n-1] + a2*y[n-2] + b0*u[n]

            // Coefficients for bilinear approximation
           
            // _w0: natural frequency
            // _z : damping ratio
            // _k : system gain

            double t2 = elapsedTime * elapsedTime;
            double w2 = _w0 * _w0;
            double w2t2 = w2 * t2;
            double tmp = 4.0 * _z * _w0 * elapsedTime + w2t2;

            double a0 = 4.0 + tmp;
            double a1 = (-8.0 + 2.0 * w2t2) / a0;
            double a2 = (4.0 - tmp) / a0;
            double b0 = _k * w2t2 / a0;

            double output = -a1 * _previousOutput1 - a2 * _previousOutput2 + b0 * input;

            // Update state
            _previousOutput2 = _previousOutput1;
            _previousOutput1 = output;

            return output;
        }

        public override Dictionary<string, double> GetParameters()
        {
            return new Dictionary<string, double>
            {
                { K_KEY, _k },
                { W0_KEY, _w0 },
                { Z_KEY, _z }
            };
        }

        public override bool GetParameter(string param, out double value)
        {
            switch (param)
            {
                case K_KEY: value = _k; return true;
                case W0_KEY: value = _w0; return true;
                case Z_KEY: value = _z; return true;
                default: value = 0.0; return false;
            }
        }

        public override bool SetParameter(string param, double value)
        {
            switch (param)
            {
                case K_KEY: K = value; return true;
                case W0_KEY: W0 = value; return true;
                case Z_KEY: Z = value; return true;
                default: return false;
            }
        }

        public override bool IsValidValue(string param, double value)
        {
            switch (param)
            {
                case K_KEY:
                    return true; // K can be any double
                case W0_KEY:
                    return value > 0.0; // W0 must be positive
                case Z_KEY:
                    return value >= 0.0; // Z must be non-negative
                default:
                    return false; // Unknown parameter
            }
        }
    }
}
