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

        public float LastOutput { get; private set; }
        public float Setpoint { get; set; }
        public float LastError { get; private set; }
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
        private float _measurement;
        private float _command;

        public Simulator(ModelType modelType, bool usePid)
        {
            _modelType = modelType;
            _model = null;
            Pid = new PidController(1f, 0f, 0f); // set PID before BuildModel
            BuildModel();
            _usePid = usePid;
            Setpoint = 0f;
            LastOutput = 0f;
            LastError = 0f;
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
                case ModelType.FirstOrder:
                    Model = new FirstOrderModel();
                    break;
                case ModelType.SecondOrder:
                    Model = new SecondOrderModel();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void BuildName(out string name)
        {
            if (_modelType is ModelType.FirstOrder)
                name = "First Order";
            else if (_modelType is ModelType.SecondOrder)
                name = "Second Order";
            else
                name = Model.GetType().Name;
            if (_usePid)
                name += " + PID";
        }

        public void BuildParams(out string param)
        {
            if (Model is FirstOrderModel firstOrderModel)
                param = $"K={firstOrderModel.K:0.###}, T={firstOrderModel.T:0.###}";
            else if (Model is SecondOrderModel secondOrderModel)
                param = $"K={secondOrderModel.K:0.###}, W0={secondOrderModel.W0:0.###}, Z={secondOrderModel.Z:0.###}";
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

        /// <summary>
        /// Called by SimulationManager each tick.
        /// Returns the new output.
        /// </summary>
        public float Step(float setpoint, float dtSeconds)
        {
            if (!IsStarted)
                return 0f;
            Setpoint = setpoint;
            return Step(dtSeconds);
        }

        public float Step(float dtSeconds)
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
            LastOutput = 0f;
            //Setpoint = 0f;
            LastError = 0f;
            LastComputeMicroseconds = 0UL;
        }
    }
}
