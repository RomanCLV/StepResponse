namespace StepResponse.ViewModels
{
    internal class EditableParameter : ViewModelBase
    {
        public string Key { get; }

        private double _value;
        public double Value
        {
            get => _value;
            set => SetValue(ref _value, value);
        }

        public EditableParameter(string key, double value)
        {
            Key = key;
            _value = value;
        }
    }
}
