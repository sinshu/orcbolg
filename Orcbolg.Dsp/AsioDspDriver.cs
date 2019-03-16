using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave.Asio;
using NAudioAsioDriver = NAudio.Wave.Asio.AsioDriver;
using NAudioAsioDriverExt = NAudio.Wave.Asio.AsioDriverExt;

namespace Orcbolg.Dsp
{
    public sealed class AsioDspDriver : IDspDriver
    {
        private readonly string driverName;
        private readonly int sampleRate;
        private readonly int bufferLength;

        private readonly int asioInputChannelOffset;
        private readonly int asioInputChannelCount;
        private readonly int[] asioInputChannelIndices;

        private readonly int asioOutputChannelOffset;
        private readonly int asioOutputChannelCount;
        private readonly int[] asioOutputChannelIndices;

        private readonly bool useLongInterval;

        private readonly List<IRealtimeDsp> realtimeDsps;
        private readonly List<INonrealtimeDsp> nonrealtimeDsps;

        private NAudioAsioDriver naDriver;
        private NAudioAsioDriverExt naDriverExt;

        private readonly int intervalLength;
        private readonly DspBuffer buffer;

        private DspState state;

        public AsioDspDriver(AsioDspSetting setting)
        {
            try
            {
                if (setting.InputChannels.Count == 0 && setting.OutputChannels.Count == 0)
                {
                    throw new ArgumentException("At least one input or output channel must be selected.");
                }

                driverName = setting.DriverName;
                sampleRate = setting.SampleRate;
                bufferLength = setting.BufferLength;

                if (setting.InputChannels.Count > 0)
                {
                    asioInputChannelOffset = setting.InputChannels.Min();
                    asioInputChannelCount = setting.InputChannels.Max() - asioInputChannelOffset + 1;
                    asioInputChannelIndices = setting.InputChannels.Select(ch => ch - asioInputChannelOffset).ToArray();
                }
                else
                {
                    asioInputChannelOffset = 0;
                    asioInputChannelCount = 0;
                    asioInputChannelIndices = Array.Empty<int>();
                }

                if (setting.OutputChannels.Count > 0)
                {
                    asioOutputChannelOffset = setting.OutputChannels.Min();
                    asioOutputChannelCount = setting.OutputChannels.Max() - asioOutputChannelOffset + 1;
                    asioOutputChannelIndices = setting.OutputChannels.Select(ch => ch - asioOutputChannelOffset).ToArray();
                }
                else
                {
                    asioOutputChannelOffset = 0;
                    asioOutputChannelCount = 0;
                    asioOutputChannelIndices = Array.Empty<int>();
                }

                useLongInterval = setting.UseLongInterval;

                realtimeDsps = new List<IRealtimeDsp>();
                nonrealtimeDsps = new List<INonrealtimeDsp>();

                naDriver = NAudioAsioDriver.GetAsioDriverByName(driverName);
                naDriverExt = new NAudioAsioDriverExt(naDriver);

                for (var ch = 0; ch < asioInputChannelIndices.Length; ch++)
                {
                    var info = naDriverExt.Capabilities.InputChannelInfos[asioInputChannelIndices[ch]];
                    if (info.type != AsioSampleType.Int32LSB)
                    {
                        throw new NotSupportedException("ASIO sample type must be Int32LSB.");
                    }
                }

                for (var ch = 0; ch < asioOutputChannelIndices.Length; ch++)
                {
                    var info = naDriverExt.Capabilities.OutputChannelInfos[asioOutputChannelIndices[ch]];
                    if (info.type != AsioSampleType.Int32LSB)
                    {
                        throw new NotSupportedException("ASIO sample type must be Int32LSB.");
                    }
                }

                naDriverExt.SetSampleRate(sampleRate);
                intervalLength = naDriverExt.CreateBuffers(asioOutputChannelCount, asioInputChannelCount, useLongInterval);
                naDriverExt.SetChannelOffset(asioOutputChannelOffset, asioInputChannelOffset);
                var entryCount = (int)Math.Ceiling((double)bufferLength / intervalLength);
                buffer = new DspBuffer(asioInputChannelIndices.Length, asioOutputChannelIndices.Length, intervalLength, entryCount);

                state = DspState.Initialized;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public static IEnumerable<string> EnumerateDriverNames()
        {
            return NAudioAsioDriver.GetAsioDriverNames();
        }

        public void AddDsp(IRealtimeDsp dsp)
        {
            CheckDisposed();

            if (state != DspState.Initialized)
            {
                throw new InvalidOperationException("AddDsp method must be called before Run method is called.");
            }

            realtimeDsps.Add(dsp);
        }

        public void AddDsp(INonrealtimeDsp dsp)
        {
            CheckDisposed();

            if (state != DspState.Initialized)
            {
                throw new InvalidOperationException("AddDsp method must be called before Run method is called.");
            }

            nonrealtimeDsps.Add(dsp);
        }

        public IDspContext Run()
        {
            CheckDisposed();

            if (state == DspState.Initialized || state == DspState.Stop)
            {
                return new AsioDspContext(this);
            }
            else
            {
                throw new InvalidOperationException("Run method must be called when DSP is not running.");
            }
        }

        public string GetInputChannelName(int channel)
        {
            return naDriverExt.Capabilities.InputChannelInfos[asioInputChannelOffset + asioInputChannelIndices[channel]].name;
        }

        public string GetOutputChannelName(int channel)
        {
            return naDriverExt.Capabilities.OutputChannelInfos[asioOutputChannelOffset + asioOutputChannelIndices[channel]].name;
        }

        public void Dispose()
        {
            if (state == DspState.Disposed)
            {
                return;
            }

            if (naDriverExt != null)
            {
                naDriverExt.ReleaseDriver();
                naDriver = null;
                naDriverExt = null;
            }

            if (naDriver != null)
            {
                naDriver.DisposeBuffers();
                naDriver.ReleaseComAsioDriver();
                naDriver = null;
            }

            state = DspState.Disposed;
        }

        private void CheckDisposed()
        {
            if (state == DspState.Disposed)
            {
                throw new ObjectDisposedException(nameof(AsioDspDriver));
            }
        }

        public string DriverName => driverName;
        public int SampleRate => sampleRate;
        public int InputChannelCount => asioInputChannelIndices.Length;
        public int OutputChannelCount => asioOutputChannelIndices.Length;
        public int IntervalLength => intervalLength;



        private sealed class AsioDspContext : IDspContext
        {
            private readonly AsioDspDriver driver;

            private readonly DspScheduler scheduler;
            private readonly Stopwatch stopwatch;

            private bool stopped;
            private long processedSampleCount;

            private readonly Task completion;

            public AsioDspContext(AsioDspDriver driver)
            {
                this.driver = driver;

                scheduler = new DspScheduler(this, driver.buffer, driver.nonrealtimeDsps);
                stopwatch = new Stopwatch();

                stopped = false;
                processedSampleCount = 0;

                completion = Run();
            }

            public async Task Run()
            {
                try
                {
                    driver.state = DspState.Running;

                    driver.buffer.Reset();
                    stopwatch.Start();

                    driver.naDriverExt.FillBufferCallback = FillBufferCallback;
                    driver.naDriverExt.Start();

                    scheduler.Start();
                    await scheduler.RealtimeDspCompletion;

                    // Since the driver might be disposed while awaiting, the null check is necessary.
                    if (driver.naDriverExt != null)
                    {
                        driver.naDriverExt.Stop();
                    }

                    await scheduler.NonrealtimeDspCompletion;
                }
                finally
                {
                    if (driver.naDriverExt != null)
                    {
                        driver.naDriverExt.FillBufferCallback = null;
                    }

                    stopwatch.Stop();

                    // If the driver is already disposed, the state should be kept as disposed.
                    if (driver.state != DspState.Disposed)
                    {
                        driver.state = DspState.Stop;
                    }
                }
            }

            private void FillBufferCallback(IntPtr[] inputChannels, IntPtr[] outputChannels)
            {
                try
                {
                    if (stopped)
                    {
                        for (var ch = 0; ch < driver.asioOutputChannelIndices.Length; ch++)
                        {
                            unsafe
                            {
                                var p = (int*)outputChannels[driver.asioOutputChannelIndices[ch]];
                                for (var t = 0; t < driver.buffer.IntervalLength; t++)
                                {
                                    p[t] = 0;
                                }
                            }
                        }
                        return;
                    }

                    var entry = driver.buffer.StartWriting();
                    if (entry == null)
                    {
                        stopped = true;
                        throw new DspException("Buffer length is not sufficient.");
                    }
                    entry.Position = processedSampleCount;
                    entry.DspStartTime = stopwatch.Elapsed;
                    entry.GlobalTime = DateTime.Now;

                    for (var ch = 0; ch < driver.asioInputChannelIndices.Length; ch++)
                    {
                        unsafe
                        {
                            var p = (int*)inputChannels[driver.asioInputChannelIndices[ch]];
                            var interval = entry.InputInterval[ch];
                            for (var t = 0; t < driver.buffer.IntervalLength; t++)
                            {
                                interval[t] = (float)p[t] / 0x80000000L;
                            }
                        }
                    }

                    foreach (var realtimeDsp in driver.realtimeDsps)
                    {
                        realtimeDsp.Process(entry.InputInterval, entry.OutputInterval, driver.buffer.IntervalLength);
                    }

                    for (var ch = 0; ch < driver.asioOutputChannelIndices.Length; ch++)
                    {
                        unsafe
                        {
                            var p = (int*)outputChannels[driver.asioOutputChannelIndices[ch]];
                            var interval = entry.OutputInterval[ch];
                            for (var t = 0; t < driver.buffer.IntervalLength; t++)
                            {
                                var value = (long)(0x80000000L * interval[t]);
                                if (value > int.MaxValue)
                                {
                                    value = int.MaxValue;
                                }
                                else if (value < int.MinValue)
                                {
                                    value = int.MinValue;
                                }
                                p[t] = (int)value;
                            }
                        }
                    }

                    Interlocked.Add(ref processedSampleCount, driver.buffer.IntervalLength);

                    entry.DspEndTime = stopwatch.Elapsed;
                    driver.buffer.EndWriting();
                }
                catch (Exception e)
                {
                    this.Stop(e);
                }
            }

            public void Post(IDspCommand command)
            {
                scheduler.Post(command);
            }

            public long ProcessedSampleCount => Interlocked.Read(ref processedSampleCount);
            public Task Completion => completion;
        }
    }
}
