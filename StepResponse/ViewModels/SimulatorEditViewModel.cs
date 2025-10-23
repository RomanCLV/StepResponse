using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using StepResponse.SimulationModel;
using System;
using System.Windows;
using System.ComponentModel;

namespace StepResponse.ViewModels
{
    internal class SimulatorEditViewModel : ViewModelBase
    {
        private readonly SimulatorViewModel _simulatorVm;

        public ObservableCollection<ModelType> ModelTypes { get; }
        public ObservableCollection<EditableParameter> ModelParameters { get; }
        public ObservableCollection<EditableParameter> PidParameters { get; }

        public Color Color { get; set; }

        private ModelType _modelType;
        public ModelType ModelType
        {
            get => _modelType;
            set
            {
                if (SetValue(ref _modelType, value))
                {
                    // --- Sauvegarde des valeurs importantes ---
                    bool gainFound = false;
                    float oldGain = 1.0f;
                    for (var i = 0; i < ModelParameters.Count; i++)
                    {
                        if (ModelParameters[i].Key == "K")
                        {
                            gainFound = true;
                            oldGain = ModelParameters[i].Value;
                            break;
                        }
                    }

                    SimulationModel.SimulationModel newModel;
                    switch (_modelType)
                    {
                        case ModelType.Linear:
                            newModel = new LinearModel();
                            break;
                        case ModelType.FirstOrder:
                            newModel = new FirstOrderModel();
                            break;
                        case ModelType.SecondOrder:
                            newModel = new SecondOrderModel();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    UninstallModelParametersPropertyChanged();

                    // --- Récupération des paramètres importants ---
                    ModelParameters.Clear();
                    var newParameters = newModel.GetParameters();
                    if (gainFound)
                        newParameters["K"] = oldGain;

                    foreach (var kv in newParameters)
                    {
                        ModelParameters.Add(new EditableParameter(kv.Key, kv.Value));
                    }

                    InstallModelParametersPropertyChanged();
                }
            }
        }

        private bool _usePid;
        public bool UsePid
        {
            get => _usePid;
            set => SetValue(ref _usePid, value);
        }

        public float Setpoint { get; set; }

        public SimulatorEditViewModel(SimulatorViewModel simulatorVm)
        {
            _simulatorVm = simulatorVm;

            // ComboBox items
            ModelTypes = new ObservableCollection<ModelType>(Enum.GetValues(typeof(ModelType)).Cast<ModelType>());

            // Parameters from model
            ModelParameters = new ObservableCollection<EditableParameter>(_simulatorVm.Simulator.Model.GetParameters().Select(kv => new EditableParameter(kv.Key, kv.Value)));

            // PID parameters
            PidParameters = new ObservableCollection<EditableParameter>(_simulatorVm.Simulator.Pid.GetParameters().Select(kv => new EditableParameter(kv.Key, kv.Value)));

            _modelType = simulatorVm.ModelType;
            Color = simulatorVm.Color;
            UsePid = simulatorVm.UsePid;
            Setpoint = simulatorVm.Setpoint;

            InstallModelParametersPropertyChanged();
        }

        private void InstallModelParametersPropertyChanged()
        {
            foreach (var param in ModelParameters)
                param.PropertyChanged += ModelParameterChanged;
        }

        private void UninstallModelParametersPropertyChanged()
        {
            foreach (var param in ModelParameters)
                param.PropertyChanged -= ModelParameterChanged;
        }

        private void ModelParameterChanged(object sender, PropertyChangedEventArgs e)
        {
            EditableParameter editableParameter = (EditableParameter)sender;

            if (!_simulatorVm.Simulator.Model.IsValidValue(editableParameter.Key, editableParameter.Value))
            {
                // if not valid, restore previous value and show error message

                // get current value from model
                _simulatorVm.Simulator.Model.GetParameter(editableParameter.Key, out float currentValue);

                // Force to raise the error (set an invalid parameter) to get the message
                try
                {
                    _simulatorVm.Simulator.Model.SetParameter(editableParameter.Key, editableParameter.Value);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Invalid value: " + ex.Message, "Error: " + ex.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error);

                    // restore previous valid value
                    editableParameter.PropertyChanged -= ModelParameterChanged;
                    editableParameter.Value = currentValue;
                    editableParameter.PropertyChanged += ModelParameterChanged;
                }
            }
        }

        public void ApplyChanges()
        {
            try
            {
                // Apply simple properties
                _simulatorVm.Color = Color;
                _simulatorVm.ModelType = ModelType;
                _simulatorVm.UsePid = UsePid;
                _simulatorVm.Setpoint = Setpoint;

                // Apply model parameters
                foreach (var kv in ModelParameters)
                    _simulatorVm.Simulator.Model.SetParameter(kv.Key, kv.Value);

                // Apply PID parameters
                foreach (var kv in PidParameters)
                    _simulatorVm.Simulator.Pid.SetParameter(kv.Key, kv.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error applying changes: " + ex.Message, "Error: " + ex.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
