using System;
using System.Windows.Media;
using StepResponse.SimulationModel;

namespace StepResponse.ViewModels
{
    internal class SimulatorViewModel : ViewModelBase
    {
        private readonly Simulator.Simulator _simulator;
        public Simulator.Simulator Simulator => _simulator;

        private Color _color;
        public Color Color
        {
            get => _color;
            set => SetValue(ref _color, value);
        }

        private string _name;
        public string Name
        {
            get => _name;
            private set => SetValue(ref _name, value);
        }

        private string _params;
        public string Params
        {
            get => _params;
            private set => SetValue(ref _params, value);
        }

        private bool _started;
        public bool Started
        {
            get => _started;
            private set
            {
                if (SetValue(ref _started, value))
                {
                    OnPropertyChanged(nameof(Stopped));
                }
            }
        }

        public bool Stopped => !_started;

        public ModelType ModelType
        {
            get => _simulator.ModelType;
            set
            {
                if (_simulator.ModelType != value)
                {
                    _simulator.ModelType = value;
                    OnPropertyChanged(nameof(ModelType));
                }
            }
        }

        public bool UsePid
        {
            get => _simulator.UsePid;
            set
            {
                if (_simulator.UsePid != value)
                {
                    _simulator.UsePid = value;
                    OnPropertyChanged(nameof(UsePid));
                }
            }
        }

        private double _lastOutput;
        public double LastOutput
        {
            get => _lastOutput;
            private set => SetValue(ref _lastOutput, value);
        }

        private double _setpoint;
        public double Setpoint
        {
            get => _setpoint;
            set
            {
                if (SetValue(ref _setpoint, value))
                    _simulator.Setpoint = _setpoint;
            }
        }


        private double _startAt;
        public double StartAt
        {
            get => _startAt;
            set
            {
                if (SetValue(ref _startAt, value))
                    _simulator.StartAt = _startAt;
            }
        }

        private double _lastError;
        public double LastError
        {
            get => _lastError;
            private set => SetValue(ref _lastError, value);
        }

        private ulong _lastComputeUs;
        public ulong LastComputeUs
        {
            get => _lastComputeUs;
            private set => SetValue(ref _lastComputeUs, value);
        }

        public SimulatorViewModel(Simulator.Simulator simulator)
        {
            Color = Colors.Blue;
            _simulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
            _simulator.BuildNameAndParams(out _name, out _params);

            _simulator.Started += Simulator_onStarted;
            _simulator.Stopped += Simulator_onStopped;
            _simulator.NameChanged += Simulator_onNameChanged;
            _simulator.ParametersChanged += Simulator_onParametersChanged;

            _lastOutput = _simulator.LastOutput;
            _setpoint = _simulator.Setpoint;
            _lastError = _simulator.LastError;
            _lastComputeUs = _simulator.LastComputeMicroseconds;
        }

        ~SimulatorViewModel()
        {
            _simulator.Started -= Simulator_onStarted;
            _simulator.Stopped -= Simulator_onStopped;
            _simulator.NameChanged -= Simulator_onNameChanged;
            _simulator.ParametersChanged -= Simulator_onParametersChanged;
        }

        private void Simulator_onNameChanged(object sender, EventArgs e)
        {
            _simulator.BuildName(out _name);
            OnPropertyChanged(nameof(Name));
        }

        private void Simulator_onParametersChanged(object sender, EventArgs e)
        {
            _simulator.BuildParams(out _params);
            OnPropertyChanged(nameof(Params));
        }

        private void Simulator_onStarted(object sender, EventArgs e)
        {
            Started = true;
        }

        private void Simulator_onStopped(object sender, EventArgs e)
        {
            Started = false;
        }

        public void Refresh()
        {
            LastOutput = _simulator.LastOutput;
            Setpoint = _simulator.Setpoint;
            LastError = _simulator.LastError;
            LastComputeUs = _simulator.LastComputeMicroseconds;
        }
    }
}
