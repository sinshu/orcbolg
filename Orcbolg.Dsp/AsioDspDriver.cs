using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
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

        private Action<IntPtr, float[], int> readDelegate;
        private Action<IntPtr, float[], int> writeDelegate;
        private Action<IntPtr, int> clearDelegate;

        private readonly int intervalLength;
        private readonly DspBuffer buffer;

        private long processedSampleCount;

        private DspState state;

        public AsioDspDriver(AsioDspSetting setting)
        {
            if (setting == null)
            {
                throw new ArgumentNullException(nameof(setting));
            }
            if (setting.InputChannels.Count == 0 && setting.OutputChannels.Count == 0)
            {
                throw new ArgumentException("At least one input or output channel must be selected.");
            }

            try
            {
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

                AsioSampleType asioSampleType;
                if (naDriverExt.Capabilities.InputChannelInfos.Length > 0)
                {
                    asioSampleType = naDriverExt.Capabilities.InputChannelInfos[0].type;
                }
                else
                {
                    asioSampleType = naDriverExt.Capabilities.OutputChannelInfos[0].type;
                }
                readDelegate = AsioSampleConverter.GetReadDelegate(asioSampleType);
                writeDelegate = AsioSampleConverter.GetWriteDelegate(asioSampleType);
                clearDelegate = AsioSampleConverter.GetClearDelegate(asioSampleType);

                naDriverExt.SetSampleRate(sampleRate);
                intervalLength = naDriverExt.CreateBuffers(asioOutputChannelCount, asioInputChannelCount, useLongInterval);
                naDriverExt.SetChannelOffset(asioOutputChannelOffset, asioInputChannelOffset);
                var entryCount = (int)Math.Ceiling((double)bufferLength / intervalLength);
                buffer = new DspBuffer(asioInputChannelIndices.Length, asioOutputChannelIndices.Length, intervalLength, entryCount);

                processedSampleCount = 0;

                state = DspState.Initialized;
            }
            catch (Exception e)
            {
                Dispose();
                ExceptionDispatchInfo.Capture(e).Throw();
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
                throw new InvalidOperationException("AddDsp must be called before Run is called.");
            }

            realtimeDsps.Add(dsp);
        }

        public void AddDsp(INonrealtimeDsp dsp)
        {
            CheckDisposed();

            if (state != DspState.Initialized)
            {
                throw new InvalidOperationException("AddDsp must be called before Run is called.");
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
                throw new InvalidOperationException("Run must be called when the driver is not running.");
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
                naDriverExt.Stop();
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

        public string DriverName
        {
            get
            {
                return driverName;
            }
        }

        public int SampleRate
        {
            get
            {
                return sampleRate;
            }
        }

        public int InputChannelCount
        {
            get
            {
                return asioInputChannelIndices.Length;
            }
        }

        public int OutputChannelCount
        {
            get
            {
                return asioOutputChannelIndices.Length;
            }
        }

        public int IntervalLength
        {
            get
            {
                return intervalLength;
            }
        }



        private sealed class AsioDspContext : IDspContext
        {
            private readonly AsioDspDriver driver;

            private readonly DspScheduler scheduler;
            private readonly Stopwatch stopwatch;

            private bool stopped;

            private readonly Task completion;

            public AsioDspContext(AsioDspDriver driver)
            {
                this.driver = driver;

                scheduler = new DspScheduler(this, driver.buffer, driver.nonrealtimeDsps);
                stopwatch = new Stopwatch();

                stopped = false;

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

                    // Since the driver might be disposed while awaiting, this null check is necessary.
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
                            driver.clearDelegate(outputChannels[driver.asioOutputChannelIndices[ch]], driver.intervalLength);
                        }
                        return;
                    }

                    var entry = driver.buffer.StartWriting();
                    if (entry == null)
                    {
                        stopped = true;
                        var e = new DspException("Buffer length is not sufficient.");
                        e.Data["thrower"] = GetType().Name;
                        throw e;
                    }
                    entry.Position = driver.processedSampleCount;
                    entry.DspStartTime = stopwatch.Elapsed;

                    for (var ch = 0; ch < driver.asioInputChannelIndices.Length; ch++)
                    {
                        driver.readDelegate(inputChannels[driver.asioInputChannelIndices[ch]], entry.InputInterval[ch], driver.intervalLength);
                    }

                    var value = 0;
                    foreach (var realtimeDsp in driver.realtimeDsps)
                    {
                        value = realtimeDsp.Process(entry.InputInterval, entry.OutputInterval, driver.buffer.IntervalLength);
                    }
                    entry.RealtimeDspReturnValue = value;

                    for (var ch = 0; ch < driver.asioOutputChannelIndices.Length; ch++)
                    {
                        driver.writeDelegate(outputChannels[driver.asioOutputChannelIndices[ch]], entry.OutputInterval[ch], driver.intervalLength);
                    }

                    Interlocked.Add(ref driver.processedSampleCount, driver.intervalLength);

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

            public long ProcessedSampleCount
            {
                get
                {
                    return Interlocked.Read(ref driver.processedSampleCount);
                }
            }

            public Task Completion
            {
                get
                {
                    return completion;
                }
            }
        }



        private static class AsioSampleConverter
        {
            public static Action<IntPtr, float[], int> GetReadDelegate(AsioSampleType asioSampleType)
            {
                switch (asioSampleType)
                {
                    case AsioSampleType.Int32LSB:
                        return Read_Int32LSB;
                    case AsioSampleType.Int32LSB24:
                        return Read_Int32LSB24;
                    default:
                        throw new DspException("ASIO sample type " + asioSampleType + " is not supported.");
                }
            }

            public static Action<IntPtr, float[], int> GetWriteDelegate(AsioSampleType asioSampleType)
            {
                switch (asioSampleType)
                {
                    case AsioSampleType.Int32LSB:
                        return Write_Int32LSB;
                    case AsioSampleType.Int32LSB24:
                        return Write_Int32LSB24;
                    default:
                        throw new DspException("ASIO sample type " + asioSampleType + " is not supported.");
                }
            }

            public static Action<IntPtr, int> GetClearDelegate(AsioSampleType asioSampleType)
            {
                switch (asioSampleType)
                {
                    case AsioSampleType.Int32LSB:
                        return Clear_Int32LSB;
                    case AsioSampleType.Int32LSB24:
                        return Clear_Int32LSB24;
                    default:
                        throw new DspException("ASIO sample type " + asioSampleType + " is not supported.");
                }
            }

            private static void Read_Int32LSB(IntPtr ptr, float[] buffer, int length)
            {
                unsafe
                {
                    var p = (int*)ptr;
                    for (var t = 0; t < length; t++)
                    {
                        buffer[t] = (float)p[t] / 0x80000000L;
                    }
                }
            }

            private static void Write_Int32LSB(IntPtr ptr, float[] buffer, int length)
            {
                unsafe
                {
                    var p = (int*)ptr;
                    for (var t = 0; t < length; t++)
                    {
                        var value = (long)(0x80000000L * buffer[t]);
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

            private static void Clear_Int32LSB(IntPtr ptr, int length)
            {
                unsafe
                {
                    var p = (int*)ptr;
                    for (var t = 0; t < length; t++)
                    {
                        p[t] = 0;
                    }
                }
            }

            // Although this implementation seems to work for ZYLIA ZM-1,
            // I'm not sure whether this is a correct way to handle Int32LSB24.
            private static void Read_Int32LSB24(IntPtr ptr, float[] buffer, int length)
            {
                unsafe
                {
                    var p = (int*)ptr;
                    for (var t = 0; t < length; t++)
                    {
                        buffer[t] = (float)(p[t] << 8) / 0x80000000L;
                    }
                }
            }

            private static void Write_Int32LSB24(IntPtr ptr, float[] buffer, int length)
            {
                unsafe
                {
                    var p = (int*)ptr;
                    for (var t = 0; t < length; t++)
                    {
                        var value = (int)(0x800000 * buffer[t]);
                        if (value > 0x7FFFFF)
                        {
                            value = 0x7FFFFF;
                        }
                        else if (value < -0x800000)
                        {
                            value = -0x800000;
                        }
                        p[t] = value;
                    }
                }
            }

            private static void Clear_Int32LSB24(IntPtr ptr, int length)
            {
                unsafe
                {
                    var p = (int*)ptr;
                    for (var t = 0; t < length; t++)
                    {
                        p[t] = 0;
                    }
                }
            }
        }



        private enum DspState
        {
            Initialized = 1,
            Running = 2,
            Stop = 3,
            Disposed = 4
        }
    }
}
