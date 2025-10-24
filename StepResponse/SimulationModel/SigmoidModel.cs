using System;
using System.Collections.Generic;

namespace StepResponse.SimulationModel
{
    /// <summary>
    /// Sigmoid growth model (logistic equation):
    ///   dy/dt = A * y * (1 - y / K)
    /// Discretized with Forward Euler:
    ///   y[n] = y[n-1] + dt * A * y[n-1] * (1 - y[n-1] / K)
    /// Produces an S-shaped time response.
    /// </summary>
    internal class SigmoidModel : SimulationModel
    {
        // Keys
        public const string K_KEY = "K";
        public const string A_KEY = "A";

        // Parameters
        private float _k; // final value (saturation)
        private float _a; // growth rate

        // State
        private float _previousOutput;

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

        public float A
        {
            get => _a;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(A), "A must be positive.");

                if (_a != value)
                {
                    _a = value;
                    ParametersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public SigmoidModel() : this(1f, 1f) { }

        public SigmoidModel(float k, float a)
        {
            _k = k;
            _a = a;
            _previousOutput = 0.001f; // éviter y=0 (bloqué)
        }

        internal override void Reset()
        {
            _previousOutput = 0.001f;
        }

        internal override void SetCurrent(float current)
        {
            _previousOutput = current;
        }

        internal override float CurrentOutput()
        {
            return _previousOutput;
        }

        internal override float Update(float input, float elapsedTime)
        {
            // On utilise l'entrée comme "activation" (0 ou 1)
            // Quand input=0 → pas de croissance
            // Quand input>0 → croissance vers K

            if (input <= 0f)
                return _previousOutput; // pas de croissance

            // Équation logistique discrète
            float y = _previousOutput;
            y += elapsedTime * _a * y * (1f - y / _k);

            // Clamp pour stabilité numérique
            y = Math.Max(0f, Math.Min(_k, y));

            _previousOutput = y;
            return y;
        }

        public override Dictionary<string, float> GetParameters()
        {
            return new Dictionary<string, float>
            {
                { K_KEY, _k },
                { A_KEY, _a },
            };
        }

        public override bool GetParameter(string param, out float value)
        {
            switch (param)
            {
                case K_KEY: value = _k; return true;
                case A_KEY: value = _a; return true;
                default: value = 0f; return false;
            }
        }

        public override bool SetParameter(string param, float value)
        {
            switch (param)
            {
                case K_KEY: K = value; return true;
                case A_KEY: A = value; return true;
                default: return false;
            }
        }

        public override bool IsValidValue(string param, float value)
        {
            switch (param)
            {
                case K_KEY: return value > 0;
                case A_KEY: return value > 0;
                default: return false;
            }
        }
    }
}
