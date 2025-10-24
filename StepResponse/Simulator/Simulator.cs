using System;
using StepResponse.Control;
using StepResponse.SimulationModel;
using SM = StepResponse.SimulationModel;

#nullable enable

namespace StepResponse.Simulator
{
    internal class Simulator
    {
        private ModelType _modelType;
        public ModelType ModelType
        {
            get => _modelType;
            set
            {
                if (!IsStarted && _modelType != value)
                {
                    _modelType = value;
                    BuildModel();
                }
            }
        }

        private SM.SimulationModel? _model;
        public SM.SimulationModel Model
        {
#pragma warning disable CS8603 // Existence possible d'un retour de référence null.
            get => _model;
#pragma warning restore CS8603 // Existence possible d'un retour de référence null.
            set
            {
                if (!IsStarted && _model != value)
                {
                    if (_model != null)
                        _model.ParametersChanged -= Model_onParametersChanged;

                    _model = value ?? throw new ArgumentNullException(nameof(Model), "Model cannot be null.");
                    _model.ParametersChanged += Model_onParametersChanged;

                    Reset();
                    NameChanged?.Invoke(this, EventArgs.Empty);
                    ParametersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public PidController Pid { get; }

        private bool _usePid;
        public bool UsePid
        {
            get => _usePid;
            set
            {
                if (!IsStarted && _usePid != value)
                {
                    _usePid = value;
                    Reset();
                    NameChanged?.Invoke(this, EventArgs.Empty);
                    ParametersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public double LastOutput { get; private set; }
        public double _startAt;
        public double StartAt
        {
            get { return _startAt; }
            set
            {
                if (_startAt != value)
                {
                    _startAt = value;
                    Model.SetCurrent(_startAt);
                }
            }
        }

        public double Setpoint { get; set; }
        public double LastError { get; private set; }
        public ulong LastComputeMicroseconds { get; private set; }

        public bool IsStarted { get; private set; }
        public EventHandler? Started;
        public EventHandler? Stopped;
        public EventHandler? NameChanged;
        public EventHandler? ParametersChanged;

        // For step
        private long _start;
        private long _end;
        private long _elapsedTicks;
        private double _measurement;
        private double _command;

        public Simulator(ModelType modelType, bool usePid)
        {
            _modelType = modelType;
            _model = null;
            Pid = new PidController(1.0, 0.0, 0.0); // set PID before BuildModel
            BuildModel();
            _usePid = usePid;
            Setpoint = 0.0;
            LastOutput = 0.0;
            LastError = 0.0;
            LastComputeMicroseconds = 0UL;

            Pid.ParametersChanged += Pid_onParametersChanged;
        }

        ~Simulator()
        {
            if (_model != null)
                _model.ParametersChanged -= Model_onParametersChanged;
            Pid.ParametersChanged -= Pid_onParametersChanged;
        }

        private void Model_onParametersChanged(object? sender, EventArgs e)
        {
            ParametersChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Pid_onParametersChanged(object? sender, EventArgs e)
        {
            ParametersChanged?.Invoke(this, EventArgs.Empty);
        }

        private void BuildModel()
        {
            switch (_modelType)
            {
                case ModelType.Linear:
                    Model = new LinearModel();
                    break;
                case ModelType.FirstOrder:
                    Model = new FirstOrderModel();
                    break;
                case ModelType.SecondOrder:
                    Model = new SecondOrderModel();
                    break;
                case ModelType.Sigmoid:
                    Model = new SigmoidModel();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void BuildName(out string name)
        {
            if (_modelType is ModelType.Linear)
                name = "Linear";
            else if (_modelType is ModelType.FirstOrder)
                name = "First Order";
            else if (_modelType is ModelType.SecondOrder)
                name = "Second Order";
            else if (_modelType is ModelType.Sigmoid)
                name = "Sigmoid";
            else
                name = Model.GetType().Name;
            if (_usePid)
                name += " + PID";
        }

        public void BuildParams(out string param)
        {
            if (Model is LinearModel linearModel)
                param = $"K={linearModel.K:0.###}, R={linearModel.R:0.###}";
            else if (Model is FirstOrderModel firstOrderModel)
                param = $"K={firstOrderModel.K:0.###}, T={firstOrderModel.T:0.###}";
            else if (Model is SecondOrderModel secondOrderModel)
                param = $"K={secondOrderModel.K:0.###}, W0={secondOrderModel.W0:0.###}, Z={secondOrderModel.Z:0.###}";
            else if (Model is SigmoidModel sigmoidModel)
                param = $"K={sigmoidModel.K:0.###}, A={sigmoidModel.A:0.###}";
            else
                param = "";
            if (_usePid)
                param += (string.IsNullOrEmpty(param) ? "" : "\n") + $"Kp={Pid.Kp:0.###}, Ki={Pid.Ki:0.###}, Kd={Pid.Kd:0.###}";
        }

        public void BuildNameAndParams(out string name, out string param)
        {
            BuildName(out name);
            BuildParams(out param);
        }

        public void Start()
        {
            if (!IsStarted)
            {
                IsStarted = true;
                Started?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Stop()
        {
            if (IsStarted)
            {
                IsStarted = false;
                Stopped?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SetCurrentOutputTo(double value)
        {
            Model.SetCurrent(value);
        }

        /// <summary>
        /// Called by SimulationManager each tick.
        /// Returns the new output.
        /// </summary>
        public double Step(double setpoint, double dtSeconds)
        {
            if (!IsStarted)
                return 0.0;
            Setpoint = setpoint;
            return Step(dtSeconds);
        }

        public double Step(double dtSeconds)
        {
            _start = System.Diagnostics.Stopwatch.GetTimestamp();

            _measurement = Model.CurrentOutput();
            LastError = Setpoint - _measurement;
            _command = UsePid ? Pid.Compute(Setpoint, _measurement, dtSeconds) : Setpoint;

            LastOutput = Model.Update(_command, dtSeconds);

            _end = System.Diagnostics.Stopwatch.GetTimestamp();
            _elapsedTicks = _end - _start;
            LastComputeMicroseconds = (ulong)((_elapsedTicks * 1_000_000L) / System.Diagnostics.Stopwatch.Frequency);

            return LastOutput;
        }

        public void Reset()
        {
            Model.Reset();
            Pid.Reset();
            LastOutput = 0.0;
            //Setpoint = 0.0;
            LastError = 0.0;
            LastComputeMicroseconds = 0UL;
        }
    }
}
