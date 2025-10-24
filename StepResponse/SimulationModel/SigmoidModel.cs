using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StepResponse.SimulationModel
{
    /// <summary>
    /// Sigmoid time transition model.
    /// At each change of input, generates a smooth S-shaped transition
    /// between previous and new input using a logistic function.
    /// 
    /// y(t) = y0 + (target - y0) / (1 + exp(-A*(t_norm - 0.5)))
    /// where t_norm ∈ [0,1] increases with elapsed time / T.
    /// </summary>
    internal class SigmoidModel : SimulationModel
    {
        public const string K_KEY = "K";
        public const string T_KEY = "T";   // transition duration
        public const string A_KEY = "A";   // steepness
        public const string H_KEY = "H";   // Half ( inflection point)

        private double _k;
        private double _t;          // total duration in seconds
        private double _a;          // slope parameter (steepness)
        private double _h;          // half point

        private double _y;                 // current output
        private double _yStart;            // value at transition start
        private double _target;            // target value
        private double _elapsed;           // time elapsed in current transition

        // Properties
        public override string Name => "Sigmoid";

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
                if (value <= 0.0) throw new ArgumentOutOfRangeException(nameof(T));
                _t = value;
                ParametersChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public double A
        {
            get => _a;
            set
            {
                if (value <= 0.0) throw new ArgumentOutOfRangeException(nameof(A));
                _a = value;
                ParametersChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public double H
        {
            get => _h;
            set
            {
                if (value < 0.0 || value > 1.0) throw new ArgumentOutOfRangeException(nameof(H));
                _h = value;
                ParametersChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public SigmoidModel() : this(1.0, 1.0, 10.0, 0.5) { }

        public SigmoidModel(double k, double t, double a, double h)
        {
            _k = k;
            _t = t;
            _a = a;
            _h = h;
            _y = 0.0;
            _yStart = 0.0;
            _target = 0.0;
            _elapsed = 0.0;
        }

        internal override void Reset()
        {
            _y = 0.0;
            _yStart = 0.0;
            _target = 0.0;
            _elapsed = 0.0;
        }

        internal override void SetCurrent(double current)
        {
            _y = current;
            _yStart = current;
            _target = current;
            _elapsed = 0.0;
        }

        internal override double CurrentOutput() => _y;


        private double safeExp(double x)
        {
            // clamp exponent to avoid overflow
            const double maxExp = 700.0;
            const double minExp = -700.0;
            double xx = x;
            if (xx > maxExp) xx = maxExp;
            if (xx < minExp) xx = minExp;
            return Math.Exp(xx);
        }

        internal override double Update(double input, double elapsedTime)
        {
            // Si la consigne change, démarrer une nouvelle transition depuis la valeur courante
            if (Math.Abs(input - _target) > 1e-9)
            {
                _yStart = _y;
                _target = input;
                _elapsed = 0.0;
            }

            // Rien à faire si déjà à la cible
            if (Math.Abs(_y - _target) < 1e-9)
                return _y;

            _elapsed += elapsedTime;
            double tNorm = _t > 0.0 ? Math.Min(1.0, _elapsed / _t) : 1.0;

            // sécurité numérique pour exp
            double a = _a;

            // raw sigmoids at t=0 and t=1 (précalculés)
            // s_raw(0) = 1 / (1 + exp(-a*(0 - 0.5))) = 1 / (1 + exp(a*0.5))
            // s_raw(1) = 1 / (1 + exp(-a*(1 - 0.5))) = 1 / (1 + exp(-a*0.5))
            double sRaw0 = 1.0 / (1.0 + safeExp(a * _h));   // t=0
            double sRaw1 = 1.0 / (1.0 + safeExp(-a * _h));  // t=1

            // raw at current tNorm
            double sRawT = 1.0 / (1.0 + safeExp(-a * (tNorm - _h)));

            double denom = sRaw1 - sRaw0;

            double s = Math.Abs(denom) < 1e-9 ?
                tNorm : // a trop petit -> sigmoïde proche linéaire : retomber sur tNorm
                (sRawT - sRaw0) / denom;

            // clamp s dans [0,1] (sécurité)
            if (s < 0.0) s = 0.0;
            else if (s > 1.0) s = 1.0;

            // interpolation entre yStart et target
            _y = _yStart + (_target - _yStart) * s;

            // si transition terminée, forcer la valeur exacte
            if (tNorm >= 1.0)
                _y = _target;

            return _y;
        }

        public override Dictionary<string, double> GetParameters()
        {
            return new Dictionary<string, double>
            {
                { K_KEY, _k },
                { T_KEY, _t },
                { A_KEY, _a },
                { H_KEY, _h },
            };
        }

        public override bool GetParameter(string param, out double value)
        {
            switch (param)
            {
                case K_KEY: value = _k; return true;
                case T_KEY: value = _t; return true;
                case A_KEY: value = _a; return true;
                case H_KEY: value = _h; return true;
                default: value = 0.0; return false;
            }
        }

        public override bool SetParameter(string param, double value)
        {
            switch (param)
            {
                case K_KEY: K = value; return true;
                case T_KEY: T = value; return true;
                case A_KEY: A = value; return true;
                case H_KEY: H = value; return true;
                default: return false;
            }
        }

        public override bool IsValidValue(string param, double value)
        {
            switch (param)
            {
                case K_KEY: return true; // K can be any double
                case T_KEY: return value > 0.0;
                case A_KEY: return value > 0.0;
                case H_KEY: return value >= 0.0 && value <= 1.0;
                default: return false;
            }
        }
    }
}
