using System;
using System.Collections.Generic;

namespace StepResponse.SimulationModel
{
    /// <summary>
    /// Sigmoid (logistic) nonlinearity with first-order dynamics towards the sigmoid target:
    ///   target = K * (1 / (1 + exp(-S*(input - X0))))
    /// The output moves towards target with a max slew rate R (units per second).
    /// </summary>
    internal class SigmoidModel : SimulationModel
    {
        // Keys as const strings for GetParameter/SetParameter
        public const string K_KEY = "K";
        public const string S_KEY = "S";
        public const string X0_KEY = "X0";
        public const string R_KEY = "R";

        private float _k;
        private float _s;
        private float _x0;
        private float _r;

        // Simulation state
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

        /// <summary>
        /// Steepness of the logistic function. Must be positive.
        /// </summary>
        public float S
        {
            get => _s;
            set
            {
                if (value <= 0f)
                    throw new ArgumentOutOfRangeException(nameof(S), "S must be positive.");

                if (_s != value)
                {
                    _s = value;
                    ParametersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Midpoint (horizontal shift) of the logistic function.
        /// </summary>
        public float X0
        {
            get => _x0;
            set
            {
                if (_x0 != value)
                {
                    _x0 = value;
                    ParametersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Maximum rate (units per second) the output can change towards the target.
        /// Must be positive.
        /// </summary>
        public float R
        {
            get => _r;
            set
            {
                if (value <= 0f)
                    throw new ArgumentOutOfRangeException(nameof(R), "R must be positive.");

                if (_r != value)
                {
                    _r = value;
                    ParametersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        // Constructors
        public SigmoidModel() : this(1f, 1f, 0f, 1f) { }

        public SigmoidModel(float k, float s, float x0, float r)
        {
            if (s <= 0f) throw new ArgumentOutOfRangeException(nameof(s), "S must be positive.");
            if (r <= 0f) throw new ArgumentOutOfRangeException(nameof(r), "R must be positive.");

            _k = k;
            _s = s;
            _x0 = x0;
            _r = r;

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
        /// Compute logistic target and move previous output towards it with max slew R.
        /// </summary>
        /// <param name="input">Current input value (u[n])</param>
        /// <param name="elapsedTime">Sampling interval (dt)</param>
        /// <returns>Current output value (y[n])</returns>
        internal override float Update(float input, float elapsedTime)
        {
            // logistic: L(x) = 1 / (1 + exp(-S*(x - X0)))
            // target = K * L(input)
            double exponent = -_s * (input - _x0);
            float logistic = (float)(1.0 / (1.0 + Math.Exp(exponent)));
            float target = _k * logistic;

            // move towards target with maximum rate _r (units per second)
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
                { S_KEY, _s },
                { X0_KEY, _x0 },
                { R_KEY, _r }
            };
        }

        public override bool GetParameter(string param, out float value)
        {
            switch (param)
            {
                case K_KEY: value = _k; return true;
                case S_KEY: value = _s; return true;
                case X0_KEY: value = _x0; return true;
                case R_KEY: value = _r; return true;
                default: value = 0f; return false;
            }
        }

        public override bool SetParameter(string param, float value)
        {
            switch (param)
            {
                case K_KEY: K = value; return true;
                case S_KEY: S = value; return true;
                case X0_KEY: X0 = value; return true;
                case R_KEY: R = value; return true;
                default: return false;
            }
        }

        public override bool IsValidValue(string param, float value)
        {
            switch (param)
            {
                case K_KEY: return true; // K any float
                case S_KEY: return value > 0f; // S must be positive
                case X0_KEY: return true; // X0 any float
                case R_KEY: return value > 0f; // R must be positive
                default: return false;
            }
        }
    }
}