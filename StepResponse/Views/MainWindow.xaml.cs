using System.Windows;
using System.Windows.Controls;
using StepResponse.ViewModels;

namespace StepResponse.Views
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is SimulatorViewModel vm)
            {
                var editWindow = new SimulatorEditWindow();
                var editVm = new SimulatorEditViewModel(vm);
                editWindow.SetViewModel(editVm);
                editWindow.Owner = this;
                editWindow.ShowDialog();
            }
        }
    }
}
