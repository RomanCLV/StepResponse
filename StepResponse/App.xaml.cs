using System.Windows;
using StepResponse.Simulator;
using StepResponse.ViewModels;
using StepResponse.Views;

namespace StepResponse
{
    /// <summary>
    /// Logique d'interaction pour App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly MainWindowViewModel _viewModel;
        private readonly SimulationManager _simulationManager;

        public App()
        {
            _simulationManager = new SimulationManager(0.010f, 0f);
            _viewModel = new MainWindowViewModel(_simulationManager);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            MainWindow mainWindow = new MainWindow
            {
                DataContext = _viewModel
            };
            mainWindow.Show();
        }
    }
}
