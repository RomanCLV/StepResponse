using System.Windows;
using StepResponse.ViewModels;

#nullable enable

namespace StepResponse.Views
{
    /// <summary>
    /// Logique d'interaction pour SimulatorEditWindow.xaml
    /// </summary>
    public partial class SimulatorEditWindow : Window
    {
        private SimulatorEditViewModel? _vm;

        public SimulatorEditWindow()
        {
            InitializeComponent();
        }

        internal void SetViewModel(SimulatorEditViewModel vm)
        {
            _vm = vm;
            DataContext = _vm;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            _vm?.ApplyChanges();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
