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
        private double _k; // final value (saturation)
        private double _a; // growth rate

        // State
        private double _previousOutput;

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

        public double A
        {
            get => _a;
            set
            {
                if (value <= 0.0)
                    throw new ArgumentOutOfRangeException(nameof(A), "A must be positive.");

                if (_a != value)
                {
                    _a = value;
                    ParametersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public SigmoidModel() : this(1.0, 1.0) { }

        public SigmoidModel(double k, double a)
        {
            _k = k;
            _a = a;
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

        internal override double Update(double input, double elapsedTime)
        {
            // On utilise l'entrée comme "activation" (0 ou 1)
            // Quand input=0 -> pas de croissance
            // Quand input>0 -> croissance vers K

            if (input <= 0.0)
                return _previousOutput; // pas de croissance

            // Équation logistique discrète
            double y = _previousOutput == 0.0 ? 0.001 : _previousOutput; // pour éviter y=0 (bloqué)
            y += elapsedTime * _a * y * (1.0 - y / _k);

            // Clamp pour stabilité numérique
            y = Math.Max(0.0, Math.Min(_k, y));

            _previousOutput = y;
            return y;
        }

        public override Dictionary<string, double> GetParameters()
        {
            return new Dictionary<string, double>
            {
                { K_KEY, _k },
                { A_KEY, _a },
            };
        }

        public override bool GetParameter(string param, out double value)
        {
            switch (param)
            {
                case K_KEY: value = _k; return true;
                case A_KEY: value = _a; return true;
                default: value = 0.0; return false;
            }
        }

        public override bool SetParameter(string param, double value)
        {
            switch (param)
            {
                case K_KEY: K = value; return true;
                case A_KEY: A = value; return true;
                default: return false;
            }
        }

        public override bool IsValidValue(string param, double value)
        {
            switch (param)
            {
                case K_KEY: return value > 0.0;
                case A_KEY: return value > 0.0;
                default: return false;
            }
        }
    }
}
