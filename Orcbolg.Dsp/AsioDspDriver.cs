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

        private bool started;

        public AsioDspDriver(AsioDspSetting setting)
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
        }

        public static IEnumerable<string> EnumerateDriverNames()
        {
            return NAudioAsioDriver.GetAsioDriverNames();
        }

        public void AddDsp(IRealtimeDsp dsp)
        {
            if (started)
            {
                throw new InvalidOperationException("AddDsp() must be called before Run() is called.");
            }

            realtimeDsps.Add(dsp);
        }

        public void AddDsp(INonrealtimeDsp dsp)
        {
            if (started)
            {
                throw new InvalidOperationException("AddDsp() must be called before Run() is called.");
            }

            nonrealtimeDsps.Add(dsp);
        }

        public IDspContext Run()
        {
            started = true;
            return new AsioDspContext(this);
        }

        public string DriverName => driverName;
        public int SampleRate => sampleRate;
        public int InputChannelCount => asioInputChannelIndices.Length;
        public int OutputChannelCount => asioOutputChannelIndices.Length;



        private sealed class AsioDspContext : IDspContext
        {
            private readonly AsioDspDriver driver;

            private readonly Task completion;

            private DspBuffer buffer;
            private DspScheduler scheduler;
            private Stopwatch stopwatch;
            private long processedSampleCount;
            private bool stopped;

            public AsioDspContext(AsioDspDriver driver)
            {
                this.driver = driver;

                completion = Run();
            }

            public async Task Run()
            {
                NAudioAsioDriver naDriver = null;
                NAudioAsioDriverExt naDriverExt = null;

                try
                {
                    naDriver = NAudioAsioDriver.GetAsioDriverByName(driver.driverName);
                    naDriverExt = new NAudioAsioDriverExt(naDriver);

                    for (var ch = 0; ch < driver.asioInputChannelIndices.Length; ch++)
                    {
                        var info = naDriverExt.Capabilities.InputChannelInfos[driver.asioInputChannelIndices[ch]];
                        if (info.type != AsioSampleType.Int32LSB)
                        {
                            throw new NotSupportedException("ASIO sample type must be Int32LSB");
                        }
                    }

                    for (var ch = 0; ch < driver.asioOutputChannelIndices.Length; ch++)
                    {
                        var info = naDriverExt.Capabilities.OutputChannelInfos[driver.asioOutputChannelIndices[ch]];
                        if (info.type != AsioSampleType.Int32LSB)
                        {
                            throw new NotSupportedException("ASIO sample type must be Int32LSB");
                        }
                    }

                    naDriverExt.SetSampleRate(driver.sampleRate);
                    naDriverExt.FillBufferCallback = FillBufferCallback;
                    var intervalLength = naDriverExt.CreateBuffers(driver.asioOutputChannelCount, driver.asioInputChannelCount, driver.useLongInterval);
                    naDriverExt.SetChannelOffset(driver.asioOutputChannelOffset, driver.asioInputChannelOffset);

                    var entryCount = (int)Math.Ceiling((double)driver.bufferLength / intervalLength);
                    buffer = new DspBuffer(driver.asioInputChannelIndices.Length, driver.asioOutputChannelIndices.Length, intervalLength, entryCount);
                    scheduler = new DspScheduler(this, buffer, driver.nonrealtimeDsps);
                    stopwatch = new Stopwatch();
                    stopwatch.Start();
                    processedSampleCount = 0;
                    stopped = false;
                    naDriverExt.Start();

                    await scheduler.RealtimeDspCompletion;

                    naDriverExt.Stop();
                    stopwatch.Stop();

                    await scheduler.NonrealtimeDspCompletion;
                }
                finally
                {
                    if (naDriverExt != null)
                    {
                        naDriverExt.ReleaseDriver();
                    }
                    else if (naDriver != null)
                    {
                        naDriver.DisposeBuffers();
                        naDriver.ReleaseComAsioDriver();
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
                                for (var t = 0; t < buffer.IntervalLength; t++)
                                {
                                    p[t] = 0;
                                }
                            }
                        }
                        return;
                    }

                    var entry = buffer.StartWriting();
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
                            for (var t = 0; t < buffer.IntervalLength; t++)
                            {
                                interval[t] = (float)p[t] / 0x80000000L;
                            }
                        }
                    }

                    foreach (var realtimeDsp in driver.realtimeDsps)
                    {
                        realtimeDsp.Process(entry.InputInterval, entry.OutputInterval, buffer.IntervalLength);
                    }

                    for (var ch = 0; ch < driver.asioOutputChannelIndices.Length; ch++)
                    {
                        unsafe
                        {
                            var p = (int*)outputChannels[driver.asioOutputChannelIndices[ch]];
                            var interval = entry.OutputInterval[ch];
                            for (var t = 0; t < buffer.IntervalLength; t++)
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

                    Interlocked.Add(ref processedSampleCount, buffer.IntervalLength);

                    entry.DspEndTime = stopwatch.Elapsed;
                    buffer.EndWriting();
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
