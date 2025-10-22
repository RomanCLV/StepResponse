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
        private float _kp;
        public float Kp
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

        private float _ki;
        public float Ki
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

        private float _kd;
        public float Kd
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
        public float OutputMin { get; set; } = float.NegativeInfinity;
        public float OutputMax { get; set; } = float.PositiveInfinity;
        public float IntegralMin { get; set; } = float.NegativeInfinity;
        public float IntegralMax { get; set; } = float.PositiveInfinity;

        // Filtre du terme dérivé : time constant tau (seconds). Si tau == 0 => pas de filtrage (derivative pur).
        // On applique un filtre passe-bas simple: d_filtered = alpha * d_prev + (1-alpha) * d_raw
        public float DerivativeFilterTau { get; set; } = 0.01f;

        // Internal state
        private float _integral;
        private float _previousError;
        private float _previousDerivativeFiltered;
        private bool _firstCompute = true;

        public PidController(float kp = 1f, float ki = 0f, float kd = 0f)
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
            _integral = 0f;
            _previousError = 0f;
            _previousDerivativeFiltered = 0f;
            _firstCompute = true;
        }

        /// <summary>
        /// Compute command from an error signal: u = Kp*e + Ki*integral(e) + Kd*derivative(e)
        /// Anti-windup by clamping the integral; output is clamped to [OutputMin, OutputMax].
        /// dt must be > 0.
        /// </summary>
        public float ComputeFromError(float error, float dt)
        {
            if (dt <= 0f)
                throw new ArgumentException("dt must be > 0", nameof(dt));

            // Proportional
            float p = _kp * error;

            // Integral (accumulate error*dt)
            _integral += error * dt;
            // Clamp integral to avoid windup
            if (_integral > IntegralMax) _integral = IntegralMax;
            else if (_integral < IntegralMin) _integral = IntegralMin;
            float i = _ki * _integral;

            // Derivative (on error)
            float derivativeRaw = 0f;
            if (_firstCompute)
            {
                // At first compute, derivative unknown: assume zero derivative to avoid spike.
                derivativeRaw = 0f;
                _firstCompute = false;
            }
            else
            {
                derivativeRaw = (error - _previousError) / dt;
            }

            // Apply simple first-order low-pass filter to derivative:
            float dFiltered;
            if (DerivativeFilterTau <= 0f)
            {
                dFiltered = derivativeRaw;
            }
            else
            {
                float alpha = DerivativeFilterTau / (DerivativeFilterTau + dt); // alpha in (0,1)
                dFiltered = alpha * _previousDerivativeFiltered + (1f - alpha) * derivativeRaw;
            }
            float d = _kd * dFiltered;

            // PID output (before saturation)
            float output = p + i + d;

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
        public float Compute(float setpoint, float measurement, float dt)
        {
            return ComputeFromError(setpoint - measurement, dt);
        }

        /// <summary>
        /// Optionally expose the internal integral (for diagnostics).
        /// </summary>
        public float GetIntegral() => _integral;

        public Dictionary<string, float> GetParameters()
        {
            return new Dictionary<string, float>
            {
                { KP_KEY, _kp },
                { KI_KEY, _ki },
                { KD_KEY, _kd },
            };
        }

        public bool GetParameter(string param, out float value)
        {
            switch (param)
            {
                case KP_KEY: value = _kp; return true;
                case KI_KEY: value = _ki; return true;
                case KD_KEY: value = _kd; return true;
                default: value = 0f; return false;
            }
        }

        public bool SetParameter(string param, float value)
        {
            switch (param)
            {
                case KP_KEY: Kp = value; return true;
                case KI_KEY: Ki = value; return true;
                case KD_KEY: Kd = value; return true;
                default: return false;
            }
        }

        public bool IsValidValue(string param, float value)
        {
            // All float values are valid for Kp, Ki, Kd
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
