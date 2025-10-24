using System;
using System.Collections.Generic;

namespace StepResponse.SimulationModel
{
    /// <summary>
    /// Linear model system.
    /// Discretized:
    ///  y[n] = y[n-1] + r * dt
    /// </summary>
    internal class LinearModel : SimulationModel
    {
        // Keys as const strings for GetParameter/SetParameter
        public const string K_KEY = "K";
        public const string R_KEY = "R";

        private double _k;
        private double _r;

        // Simulation state
        private double _previousOutput; // y[n-1]

        // Properties
        public override string Name => "Linear";

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

        public double R
        {
            get => _r;
            set
            {
                if (value <= 0.0)
                    throw new ArgumentOutOfRangeException(nameof(R), "R must be positive.");

                if (_r != value)
                {
                    _r = value;
                    ParametersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public LinearModel() : this(1.0, 1.0) { }

        public LinearModel(double k, double r)
        {
            _k = k;
            _r = r;
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
            double target = _k * input;
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

        public override Dictionary<string, double> GetParameters()
        {
            return new Dictionary<string, double>
            {
                { K_KEY, _k },
                { R_KEY, _r }
            };
        }

        public override bool GetParameter(string param, out double value)
        {
            switch (param)
            {
                case K_KEY: value = _k; return true;
                case R_KEY: value = _r; return true;
                default: value = 0.0; return false;
            }
        }

        public override bool SetParameter(string param, double value)
        {
            switch (param)
            {
                case K_KEY: K = value; return true;
                case R_KEY: R = value; return true;
                default: return false;
            }
        }

        public override bool IsValidValue(string param, double value)
        {
            switch (param)
            {
                case K_KEY: return true; // K can be any double
                case R_KEY: return value > 0.0; // R must be positive
                default: return false;
            }
        }
    }
}
