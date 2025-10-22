using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using OxyPlot;
using OxyPlot.Series;
using StepResponse.SimulationSamples;
using StepResponse.ViewModels;        // SimulatorViewModel

namespace StepResponse.ViewModels.Plotting
{
    internal class SimulationPlotViewModel : ViewModelBase
    {
        private readonly IReadOnlySimulatorSamplesManager _samplesManager;
        private readonly ObservableCollection<SimulatorViewModel> _simulators;
        private readonly Dispatcher _dispatcher;

        public PlotModel PlotModel { get; }

        // one LineSeries per simulator
        private readonly List<LineSeries> _outputSeries = new List<LineSeries>();

        // internal counter : how many samples per collection we've already consumed
        private int _previousSamplesPerCollection = 0;

        private bool _initialized = false;

        public SimulationPlotViewModel(IReadOnlySimulatorSamplesManager samplesManager,
                                       ObservableCollection<SimulatorViewModel> simulators,
                                       Dispatcher dispatcher)
        {
            _samplesManager = samplesManager ?? throw new ArgumentNullException(nameof(samplesManager));
            _simulators = simulators ?? throw new ArgumentNullException(nameof(simulators));
            _dispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;

            PlotModel = new PlotModel { Title = "Simulation outputs" };

            // Note: we DO NOT create series here. Series are created by Init(colors) at Start.
            // This avoids any assumptions about initial simulator count or colors.
        }

        /// <summary>
        /// Initialize series before the simulation starts.
        /// Provide one color per simulator (order must match simulators collection).
        /// This resets previous counters and clears any existing series/points.
        /// </summary>
        public void Init()
        {
            // Build series list on UI thread
            _dispatcher.Invoke(() =>
            {
                PlotModel.Series.Clear();
                _outputSeries.Clear();

                for (int i = 0; i < _simulators.Count; i++)
                {
                    var vm = _simulators[i];
                    var oxyColor = OxyColorFromMedia(vm.Color);

                    var series = new LineSeries
                    {
                        Title = vm.Name ?? $"Sim {vm.Name}",
                        StrokeThickness = 1.2,
                        MarkerType = MarkerType.None,
                        Color = oxyColor
                    };

                    _outputSeries.Add(series);
                    PlotModel.Series.Add(series);
                }

                PlotModel.InvalidatePlot(true);
            });

            // reset counters
            _previousSamplesPerCollection = 0;
            _initialized = true;
        }

        /// <summary>
        /// Refresh plot by adding only the new points since the last Refresh.
        /// Call this regularly from UI (e.g. from MainWindowViewModel's UI timer).
        /// </summary>
        public void Refresh()
        {
            if (!_initialized)
                return;

            // read current available count
            int samplesCount = (int)_samplesManager.SamplesPerCollection;

            // detect reset: if the samples counter decreased, clear and start over
            if (samplesCount < _previousSamplesPerCollection)
            {
                _previousSamplesPerCollection = 0;
                _dispatcher.Invoke(() =>
                {
                    foreach (var s in _outputSeries)
                        s.Points.Clear();
                    PlotModel.InvalidatePlot(true);
                });
            }

            // nothing new
            if (samplesCount == _previousSamplesPerCollection)
                return;

            int oldCount = _previousSamplesPerCollection;
            int newCount = samplesCount; // we want points in [oldCount .. newCount-1]

            int seriesToProcess = Math.Min(_outputSeries.Count, _samplesManager.CollectionsCount);

            // prepare lists of points for each series outside UI thread
            var pointsToAddPerSeries = new List<List<DataPoint>>(seriesToProcess);
            for (int i = 0; i < seriesToProcess; i++)
                pointsToAddPerSeries.Add(new List<DataPoint>());

            for (int simIndex = 0; simIndex < seriesToProcess; simIndex++)
            {
                var view = _samplesManager.GetSamples(simIndex);
                int available = Math.Min(view.Count, newCount);
                if (available <= oldCount)
                    continue; // no new data for this series

                int begin = oldCount;
                int endExclusive = available;

                var pts = new List<DataPoint>(endExclusive - begin);
                for (int sampleIndex = begin; sampleIndex < endExclusive; sampleIndex++)
                {
                    var sample = view[sampleIndex]; // direct access, no copy
                    pts.Add(new DataPoint(sample.Time, sample.Output));
                }
                pointsToAddPerSeries[simIndex] = pts;
            }

            // apply all additions in one UI dispatch
            _dispatcher.Invoke(() =>
            {
                for (int simIndex = 0; simIndex < seriesToProcess; simIndex++)
                {
                    var pts = pointsToAddPerSeries[simIndex];
                    if (pts == null || pts.Count == 0)
                        continue;
                    _outputSeries[simIndex].Points.AddRange(pts);
                }
                PlotModel.InvalidatePlot(true);
            });

            // update internal counter
            _previousSamplesPerCollection = newCount;
        }

        private static OxyColor OxyColorFromMedia(System.Windows.Media.Color c) => OxyColor.FromArgb(c.A, c.R, c.G, c.B);
    }
}
