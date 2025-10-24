using System;
using System.Collections.Generic;
using StepResponse.SimulationModel;

#nullable enable

namespace StepResponse.Control
{
    /// <summary>
    /// Discrete PID with anti-windup (clamping) and derivative filtering.
    /// Compatible with variable dt.
    /// </summary>
    internal class PidController : IParametrable
    {
        // Keys as const strings for GetParameter/SetParameter
        public const string KP_KEY = "Kp";
        public const string KI_KEY = "Ki";
        public const string KD_KEY = "Kd";

        // Gains
        private double _kp;
        public double Kp
        {
            get => _kp;
            set 
            {
                if (_kp != value)
                {
                    _kp = value;
                    ParametersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private double _ki;
        public double Ki
        {
            get => _ki;
            set
            {
                if (_ki != value)
                {
                    _ki = value;
                    ParametersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private double _kd;
        public double Kd
        {
            get => _kd;
            set
            {
                if (_kd != value)
                {
                    _kd = value;
                    ParametersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public EventHandler? ParametersChanged;

        // Anti-windup / saturation
        public double OutputMin { get; set; } = double.NegativeInfinity;
        public double OutputMax { get; set; } = double.PositiveInfinity;
        public double IntegralMin { get; set; } = double.NegativeInfinity;
        public double IntegralMax { get; set; } = double.PositiveInfinity;

        // Filtre du terme dérivé : time constant tau (seconds). Si tau == 0 => pas de filtrage (derivative pur).
        // On applique un filtre passe-bas simple: d_filtered = alpha * d_prev + (1-alpha) * d_raw
        public double DerivativeFilterTau { get; set; } = 0.01;

        // Internal state
        private double _integral;
        private double _previousError;
        private double _previousDerivativeFiltered;
        private bool _firstCompute = true;

        public PidController(double kp = 1.0, double ki = 0.0, double kd = 0.0)
        {
            _kp = kp;
            _ki = ki;
            _kd = kd;
        }

        /// <summary>
        /// Reset internal states (integral, previous error, derivative filter).
        /// </summary>
        public void Reset()
        {
            _integral = 0.0;
            _previousError = 0.0;
            _previousDerivativeFiltered = 0.0;
            _firstCompute = true;
        }

        /// <summary>
        /// Compute command from an error signal: u = Kp*e + Ki*integral(e) + Kd*derivative(e)
        /// Anti-windup by clamping the integral; output is clamped to [OutputMin, OutputMax].
        /// dt must be > 0.
        /// </summary>
        public double ComputeFromError(double error, double dt)
        {
            if (dt <= 0.0)
                throw new ArgumentException("dt must be > 0", nameof(dt));

            // Proportional
            double p = _kp * error;

            // Integral (accumulate error*dt)
            _integral += error * dt;
            // Clamp integral to avoid windup
            if (_integral > IntegralMax) _integral = IntegralMax;
            else if (_integral < IntegralMin) _integral = IntegralMin;
            double i = _ki * _integral;

            // Derivative (on error)
            double derivativeRaw = 0.0;
            if (_firstCompute)
            {
                // At first compute, derivative unknown: assume zero derivative to avoid spike.
                derivativeRaw = 0.0;
                _firstCompute = false;
            }
            else
            {
                derivativeRaw = (error - _previousError) / dt;
            }

            // Apply simple first-order low-pass filter to derivative:
            double dFiltered;
            if (DerivativeFilterTau <= 0.0)
            {
                dFiltered = derivativeRaw;
            }
            else
            {
                double alpha = DerivativeFilterTau / (DerivativeFilterTau + dt); // alpha in (0,1)
                dFiltered = alpha * _previousDerivativeFiltered + (1.0 - alpha) * derivativeRaw;
            }
            double d = _kd * dFiltered;

            // PID output (before saturation)
            double output = p + i + d;

            // Saturate output
            if (output > OutputMax) 
                output = OutputMax;
            else if (output < OutputMin)
                output = OutputMin;

            // Save state for next step
            _previousError = error;
            _previousDerivativeFiltered = dFiltered;

            return output;
        }

        /// <summary>
        /// Convenience: compute command from setpoint and measurement.
        /// Equivalent to ComputeFromError(setpoint - measurement, dt).
        /// </summary>
        public double Compute(double setpoint, double measurement, double dt)
        {
            return ComputeFromError(setpoint - measurement, dt);
        }

        /// <summary>
        /// Optionally expose the internal integral (for diagnostics).
        /// </summary>
        public double GetIntegral() => _integral;

        public Dictionary<string, double> GetParameters()
        {
            return new Dictionary<string, double>
            {
                { KP_KEY, _kp },
                { KI_KEY, _ki },
                { KD_KEY, _kd },
            };
        }

        public bool GetParameter(string param, out double value)
        {
            switch (param)
            {
                case KP_KEY: value = _kp; return true;
                case KI_KEY: value = _ki; return true;
                case KD_KEY: value = _kd; return true;
                default: value = 0.0; return false;
            }
        }

        public bool SetParameter(string param, double value)
        {
            switch (param)
            {
                case KP_KEY: Kp = value; return true;
                case KI_KEY: Ki = value; return true;
                case KD_KEY: Kd = value; return true;
                default: return false;
            }
        }

        public bool IsValidValue(string param, double value)
        {
            // All double values are valid for Kp, Ki, Kd
            switch (param)
            {
                case KP_KEY:
                case KI_KEY:
                case KD_KEY:
                    return true;
                default:
                    return false;
            }
        }
    }
}
