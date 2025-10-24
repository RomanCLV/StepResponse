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
        private double _k;
        private double _t;

        // Simulation state (previous outputs for second-order system)
        private double _previousOutput; // y[n-1]

        // Properties
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

        public double T 
        {
            get => _t;
            set
            {
                if (value <= 0.0)
                    throw new ArgumentOutOfRangeException(nameof(T), "T must be positive.");

                if (_t != value)
                {
                    _t = value;
                    ParametersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public FirstOrderModel() : this(1.0, 1.0) { }

        public FirstOrderModel(double k, double t)
        {
            _k = k;
            _t = t;
            _previousOutput = 0.0;
        }

        internal override void Reset()
        {
            _previousOutput = 0.0;
        }

        internal override void SetCurrent(double current)
        {
            _previousOutput = current;
        }

        internal override double CurrentOutput()
        {
            return _previousOutput;
        }

        /// <summary>
        /// First-order system discretization using Forward Euler.
        /// </summary>
        /// <param name="input">Current input value (u[n])</param>
        /// <param name="elapsedTime">Sampling interval (dt)</param>
        /// <returns>Current output value (y[n])</returns>
        internal override double Update(double input, double elapsedTime)
        {
            // y[n] = y[n-1] + (dt / T) * (K * u[n] - y[n-1])

            _previousOutput = _previousOutput + (elapsedTime / _t) * (_k * input - _previousOutput);
            return _previousOutput;
        }

        public override Dictionary<string, double> GetParameters()
        {
            return new Dictionary<string, double>
            {
                { K_KEY, _k },
                { T_KEY, _t }
            };
        }

        public override bool GetParameter(string param, out double value)
        {
            switch (param)
            {
                case K_KEY: value = _k; return true;
                case T_KEY: value = _t; return true;
                default: value = 0.0; return false;
            }
        }

        public override bool SetParameter(string param, double value)
        {
            switch (param)
            {
                case K_KEY: K = value; return true;
                case T_KEY: T = value; return true;
                default: return false;
            }
        }

        public override bool IsValidValue(string param, double value)
        {
            switch (param)
            {
                case K_KEY: return true; // K can be any double
                case T_KEY: return value > 0.0; // T must be positive
                default: return false;
            }
        }
    }
}
