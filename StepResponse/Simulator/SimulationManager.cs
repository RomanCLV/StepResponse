using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using StepResponse.Samples;
using StepResponse.SimulationSamples;

#nullable enable

namespace StepResponse.Simulator
{
    internal class SimulationManager : IDisposable
    {
        private readonly List<Simulator> _simulators;
        private readonly SimulatorSamplesManager _simulatorSamplesManager;
        private readonly IReadOnlySimulatorSamplesManager _readOnlySamplesManager;

        public IReadOnlySimulatorSamplesManager SamplesManager => _readOnlySamplesManager;

        private Thread? _thread;
        private CancellationTokenSource? _cts;
        private readonly object _lock;

        private float _samplingTimeSeconds;
        public float SamplingTimeSeconds
        {
            get => Volatile.Read(ref _samplingTimeSeconds);
            set => Volatile.Write(ref _samplingTimeSeconds, value);
        }

        bool _globalSetPointChanged;

        private float _globalSetpoint;
        public float GlobalSetpoint
        {
            get => _globalSetpoint;
            set
            {
                lock (_lock)
                {
                    if (_globalSetpoint != value)
                    {
                        _globalSetpoint = value;
                        if (IsRunning)
                            _globalSetPointChanged = true;
                        else
                        {
                            foreach (var sim in _simulators)
                                sim.Setpoint = value;
                        }
                    }
                }
            }
        }

        public uint SamplesPerCollection => _simulatorSamplesManager.SamplesPerCollection;

        public float SimulationTime { get; private set; }

        public bool IsRunning { get; private set; }

        public SimulationManager(float samplingTimeSecs = 0.010f, float globalSetpoint = 0f)
        {
            _lock = new object();
            _simulators = new List<Simulator>();
            _simulatorSamplesManager = new SimulatorSamplesManager();
            _readOnlySamplesManager = new ReadOnlySimulatorSamplesManager(_simulatorSamplesManager);

            SimulationTime = 0f;
            _samplingTimeSeconds = samplingTimeSecs;
            _globalSetPointChanged = false;
            _globalSetpoint = globalSetpoint;
            IsRunning = false;
        }

        public void AddSimulator(Simulator sim)
        {
            lock (_lock)
            {
                if (!IsRunning)
                    _simulators.Add(sim);
            }
        }

        public void RemoveSimulator(Simulator sim)
        {
            lock (_lock)
            {
                if (!IsRunning)
                    _simulators.Remove(sim);
            }
        }

        public IReadOnlyList<Simulator> GetSimulatorsSnapshot()
        {
            lock (_lock)
            {
                return _simulators.ToArray();
            }
        }

        public void Start()
        {
            lock (_lock)
            {
                if (IsRunning) return;

                _cts = new CancellationTokenSource();
                _thread = new Thread(() => RunLoop(_cts.Token))
                {
                    IsBackground = true,
                    Name = "SimulationThread",
                    Priority = ThreadPriority.Highest
                };
                IsRunning = true;
                _thread.Start();
            }
        }

        public void Pause()
        {
            StopThread(false);
        }

        public void Stop()
        {
            StopThread(true);
        }

        private void StopThread(bool reset)
        {
            Thread? threadToJoin = null;
            CancellationTokenSource? ctsToCancel = null;

            lock (_lock)
            {
                if (!IsRunning)
                    return;

                // capture et annule
                ctsToCancel = _cts;
                threadToJoin = _thread;

                try
                {
                    ctsToCancel?.Cancel();
                }
                catch (ObjectDisposedException) { /* éventuel, on ignore */ }
            }

            // join hors du lock (évite deadlock). Utiliser un timeout pour éviter blocage infini.
            if (threadToJoin != null && threadToJoin.IsAlive)
            {
                const int joinTimeoutMs = 5000; // 5s
                bool finished = threadToJoin.Join(joinTimeoutMs);
                if (!finished)
                {
                    Trace.TraceWarning($"Simulation thread did not stop after {joinTimeoutMs} ms.");
                }
            }

            // Mise à jour de l'état et cleanup sous lock
            lock (_lock)
            {
                IsRunning = false;
                if (reset)
                {
                    foreach (var s in _simulators)
                        s.Reset();
                }

                // Clear the references so Start can recreate them later
                _thread = null;
                try
                {
                    _cts?.Dispose();
                }
                catch { /* ignore */ }
                _cts = null;
            }
        }

        private void RunLoop(CancellationToken token)
        {
            Stopwatch sw = Stopwatch.StartNew();
            long start = Stopwatch.GetTimestamp();
            long freq = Stopwatch.Frequency;
            long lastTick = sw.ElapsedTicks;

            long dtTicks;
            long targetTick;
            long remaining;
            int ms;

            long now;
            double dtMeasuredSeconds;
            float dt = SamplingTimeSeconds;

            int simIndex;
            int simCount = _simulators.Count;
            SimulationTime = 0f;

            // Prepare samples storage
            // No need to call Clear() because Reserve() does it
            _simulatorSamplesManager.Reserve(simCount, SamplingTimeSeconds, 180f); // reserve 3 minutes of samples

            try
            {
                lock (_lock)
                {
                    foreach (Simulator sim in _simulators)
                        sim.Start();
                }

                while (!token.IsCancellationRequested)
                {
                    SimulationTime += dt;

                    // compute next wake-up time
                    dtTicks = (long)(dt * freq);
                    targetTick = lastTick + dtTicks;

                    // sleep/wait until targetTick
                    while ((remaining = targetTick - sw.ElapsedTicks) > 0)
                    {
                        ms = (int)((remaining * 1000) / freq);
                        if (ms > 1)
                        {
                            Thread.Sleep(ms - 1);
                        }
                        else
                        {
                            Thread.SpinWait(10);
                        }
                        if (token.IsCancellationRequested) break;
                    }

                    now = sw.ElapsedTicks;
                    dtMeasuredSeconds = (now - lastTick) / (double)freq;
                    lastTick = now;

                    // Step simulators
                    lock (_lock)
                    {
                        if (_globalSetPointChanged)
                        {   // apply new global setpoint to all sims
                            foreach (var sim in _simulators)
                                sim.Setpoint = _globalSetpoint;
                            _globalSetPointChanged = false;
                        }

                        for (simIndex = 0; simIndex < simCount; simIndex++)
                        {
                            _simulatorSamplesManager.AddSample(
                                simIndex,
                                SimulationTime,
                                _simulators[simIndex].Setpoint,
                                _simulators[simIndex].Step(dt), 
                                _simulators[simIndex].LastComputeMicroseconds);
                        }
                        _simulatorSamplesManager.IncreaseSamplesPerCollection();
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("SimulationManager thread error: " + ex.GetType().Name + ": " + ex.Message);
                try { _cts?.Cancel(); } catch { }
            }
            finally
            {
                lock (_lock)
                {
                    foreach (Simulator sim in _simulators)
                        sim.Stop();
                }

                // Not Clearing samples, to keep last data for display, export, ...
            }
        }

        public void Dispose()
        {
            if (IsRunning)
                Stop();
            _cts?.Dispose();
        }
    }
}
