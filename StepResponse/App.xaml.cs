using System;
using System.Diagnostics;
using System.Windows;
using StepResponse.SimulationModel;
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
            _simulationManager = new SimulationManager(0.010f, 0.0);
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

        private void computeW0()
        {
            const double dt = 0.001;
            int w0 = 100;
            double value;
            int iterations;
            double time;

            double targetTime = 0.100;
            const double targetTimeIncrement = 0.010;
            double maxTargetTime = 1.000;

            while (targetTime <= maxTargetTime)
            {
                w0 = 10;
                do
                {
                    iterations = 0;
                    time = 0.0;
                    w0++;

                    SecondOrderModel model = new SecondOrderModel(2.0, w0, 1.0);
                    Simulator.Simulator simulator = new Simulator.Simulator(ModelType.SecondOrder, false)
                    {
                        Model = model,
                        Setpoint = 1.0
                    };
                    //simulator.BuildNameAndParams(out string name, out string parameters);
                    //Trace.WriteLine($"{name}: {parameters}");

                    simulator.Start();
                    do
                    {
                        time = Math.Round((++iterations) * dt, 3);
                        value = simulator.Step(dt);
                    } while (value < 0.9999);
                    simulator.Stop();

                } while (time > targetTime);
                //Trace.WriteLine($"{targetTime};{w0}");
                Trace.WriteLine(w0);

                targetTime = Math.Round(targetTime + targetTimeIncrement, 3);
            }
        }
    }
}
