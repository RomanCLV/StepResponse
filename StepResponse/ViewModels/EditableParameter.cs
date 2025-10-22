namespace StepResponse.ViewModels
{
    internal class EditableParameter : ViewModelBase
    {
        public string Key { get; }

        private float _value;
        public float Value
        {
            get => _value;
            set => SetValue(ref _value, value);
        }

        public EditableParameter(string key, float value)
        {
            Key = key;
            _value = value;
        }
    }
}
