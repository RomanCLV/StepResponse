using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using StepResponse.SimulationModel;
using StepResponse.Simulator;
using StepResponse.ViewModels.Plotting;

#nullable enable

namespace StepResponse.ViewModels
{
    internal class MainWindowViewModel : ViewModelBase
    {
        // Champs privés
        private float _samplingTime;
        private float _globalSetpoint;
        private bool _stopped;

        private readonly SimulationManager _simulationManager;

        private readonly Dispatcher _uiDispatcher;
        private readonly DispatcherTimer _uiTimer;
        private const double UiRefreshHz = 60.0;

        // Local tracking du temps de simulation
        private volatile float _simulationElapsedSeconds;
        private volatile uint _sampleCount;

        public float SamplingTime
        {
            get => _samplingTime;
            set
            {
                // bornes utiles : 1 ms .. 1 s
                if (value < 0.001f)
                    value = 0.001f;
                else if (value > 1f)
                    value = 1f;

                if (SetValue(ref _samplingTime, value))
                    _simulationManager.SamplingTimeSeconds = value;
            }
        }

        public float GlobalSetpoint
        {
            get => _globalSetpoint;
            set
            {
                if (SetValue(ref _globalSetpoint, value))
                {
                    foreach (var sim in Simulators)
                        sim.Setpoint = value;
                    _simulationManager.GlobalSetpoint = value;
                }
            }
        }

        public float SimulationTimeDisplay
        {
            get => _simulationElapsedSeconds;
        }

        public ulong SampleCountDisplay
        {
            get => _sampleCount;
        }

        public bool Stopped
        {
            get => _stopped;
            private set
            {
                if (SetValue(ref _stopped, value))
                {
                    // Les canExecute des commandes doivent être rafraîchis
                    StartCommand?.RaiseCanExecuteChanged();
                    StopCommand?.RaiseCanExecuteChanged();
                    AddSimulatorCommand?.RaiseCanExecuteChanged();
                    ExportCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<SimulatorViewModel> Simulators { get; }

        public bool HasSimulators => Simulators.Count > 0;

        private SimulatorViewModel? _selectedSimulator;
        public SimulatorViewModel? SelectedSimulator
        {
            get => _selectedSimulator;
            set
            {
                
                if (SetValue(ref _selectedSimulator, value))
                {
                    RemoveSimulatorCommand.RaiseCanExecuteChanged();
                }
            }
        }

        //Plotting
        private readonly SimulationPlotViewModel _plotViewModel;
        public SimulationPlotViewModel PlotViewModel => _plotViewModel;

        // Commandes (RelayCommand pour pouvoir RaiseCanExecuteChanged)
        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand ExportCommand { get; }
        public RelayCommand AddSimulatorCommand { get; }
        public RelayCommand RemoveSimulatorCommand { get; }

        // Constructeur
        public MainWindowViewModel(SimulationManager simulationManager)
        {
            _simulationManager = simulationManager ?? throw new ArgumentNullException(nameof(simulationManager));

            _uiDispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

            // Timer UI (dispatcher) - 60 Hz
            _uiTimer = new DispatcherTimer(DispatcherPriority.Render, _uiDispatcher)
            {
                Interval = TimeSpan.FromSeconds(1.0 / UiRefreshHz)
            };
            _uiTimer.Tick += UiTimer_Tick;

            // valeurs par défaut
            SamplingTime = simulationManager.SamplingTimeSeconds;
            GlobalSetpoint = simulationManager.GlobalSetpoint;
            Stopped = true;

            Simulators = new ObservableCollection<SimulatorViewModel>();

            _plotViewModel = new SimulationPlotViewModel(_simulationManager.SamplesManager, Simulators, _uiDispatcher);

            StartCommand = new RelayCommand(StartSimulation, CanStartSimulation);
            StopCommand = new RelayCommand(StopSimulation, CanStopSimulation);
            ExportCommand = new RelayCommand(ExportSimulation, CanExportSimulation);
            AddSimulatorCommand = new RelayCommand(AddSimulator, () => Stopped);
            RemoveSimulatorCommand = new RelayCommand(RemoveSelectedSimulator, () => SelectedSimulator != null && Stopped);

            // Si il y a déjà des simulateurs dans le manager (ex: injection pré-remplie), on les expose
            PopulateSimulatorsFromManager();
        }

        ~MainWindowViewModel()
        {
            Dispose();
        }

        public override void Dispose()
        {
            base.Dispose();
            _uiTimer.Stop();
            _uiTimer.Tick -= UiTimer_Tick;
        }

        // ---- Command handlers ----

        private void StartSimulation()
        {
            if (!Stopped) 
                return;

            // Reset local counters
            UpdateSimulationDisplays(0f, 0);

            _plotViewModel.Init();  // prepare plot series
            _simulationManager.Start();
            _uiTimer.Start();      // start UI refresh

            Stopped = false;
        }

        private bool CanStartSimulation() => Stopped;

        private void StopSimulation()
        {
            if (Stopped) 
                return;

            _simulationManager.Stop();
            _uiTimer.Stop();      // stop UI refresh
            Stopped = true;
        }

        private bool CanStopSimulation() => !Stopped;

        private void ExportSimulation()
        {
            // Placeholder : implémente ton export ici (CSV, JSON...)
            // Exemple : demander au simulationManager de fournir un snapshot des samples et lancer l'export
            MessageBox.Show("Export not implemented yet.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool CanExportSimulation() => !Stopped; // autorise export uniquement si la simulation n'est pas en cours

        private void AddSimulator()
        {
            // Only allowed when stopped (guarded by CanExecute too)
            if (!Stopped)
                return;

            // Default creation : Linear model with PID enabled
            var sim = new Simulator.Simulator((ModelType)Enum.GetValues(typeof(ModelType)).GetValue(0), usePid: false);

            // Add to manager and to observable collection
            _simulationManager.AddSimulator(sim);

            var vm = new SimulatorViewModel(sim);
            Simulators.Add(vm);

            OnPropertyChanged(nameof(HasSimulators));
        }

        private void RemoveSelectedSimulator()
        {
            if (SelectedSimulator != null)
            {
                _simulationManager.RemoveSimulator(SelectedSimulator.Simulator);
                Simulators.Remove(SelectedSimulator);
                SelectedSimulator = null;

                OnPropertyChanged(nameof(HasSimulators));
            }
        }

        // ---- Helpers ----

        private void PopulateSimulatorsFromManager()
        {
            // If the manager already had simulators, create viewmodels
            var sims = _simulationManager.GetSimulatorsSnapshot();
            foreach (var s in sims)
            {
                Simulators.Add(new SimulatorViewModel(s));
            }
        }

        private void UiTimer_Tick(object? sender, EventArgs e)
        {
            RefreshFromSimulationManager();
            // Update plot (reads direct samples). We do it after refresh so colors/names are current.
            _plotViewModel.Refresh();
        }

        private void RefreshFromSimulationManager()
        {
            // Update local tracking of simulation time and sample count
            UpdateSimulationDisplays((float)Math.Round(_simulationManager.SimulationTime, 3), _simulationManager.SamplesPerCollection);

            // Récupérer snapshot des simulateurs (GetSimulatorsSnapshot() fait une ToArray sous lock)
            var sims = _simulationManager.GetSimulatorsSnapshot();

            // Sinon, on met à jour chaque VM (Refresh() doit seulement setter quand valeur change)
            for (int i = 0; i < sims.Count; i++)
            {
                Simulators[i].Refresh();
            }
        }

        private void UpdateSimulationDisplays(float timeElapsedSecs, uint samplesCount)
        {
            _simulationElapsedSeconds = timeElapsedSecs;
            _sampleCount = samplesCount;

            OnPropertyChanged(nameof(SimulationTimeDisplay));
            OnPropertyChanged(nameof(SampleCountDisplay));
        }
    }
}
