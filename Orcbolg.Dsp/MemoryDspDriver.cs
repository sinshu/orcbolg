using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Orcbolg.Dsp
{
    public sealed class MemoryDspDriver : IDspDriver
    {
        private readonly float[][] inputData;
        private readonly float[][] outputData;
        private readonly int sampleRate;
        private readonly int intervalLength;
        private readonly int inputChannelCount;
        private readonly int outputChannelCount;
        private readonly int dataLength;

        private readonly List<IRealtimeDsp> realtimeDsps;
        private readonly List<INonrealtimeDsp> nonrealtimeDsps;

        private long processedSampleCount;

        private DspState state;

        public MemoryDspDriver(float[][] inputData, int sampleRate, int intervalLength)
        {
            if (inputData == null) throw new ArgumentNullException(nameof(inputData));
            if (sampleRate <= 0) throw new ArgumentException("Sample rate must be greater than or equal to one.");
            if (intervalLength <= 0) throw new ArgumentException("Interval length must be greater than or equal to one.");

            this.inputData = inputData;
            this.outputData = null;
            this.sampleRate = sampleRate;
            this.intervalLength = intervalLength;
            inputChannelCount = inputData.Length;
            outputChannelCount = 0;
            dataLength = inputData[0].Length;

            realtimeDsps = new List<IRealtimeDsp>();
            nonrealtimeDsps = new List<INonrealtimeDsp>();

            processedSampleCount = 0;

            state = DspState.Initialized;
        }

        public MemoryDspDriver(float[][] inputData, float[][] outputData, int sampleRate, int intervalLength)
        {
            if (inputData == null) throw new ArgumentNullException(nameof(inputData));
            if (outputData == null) throw new ArgumentNullException(nameof(outputData));
            if (sampleRate <= 0) throw new ArgumentException("Sample rate must be greater than or equal to one.");
            if (intervalLength <= 0) throw new ArgumentException("Interval length must be greater than or equal to one.");

            this.inputData = inputData;
            this.outputData = outputData;
            this.sampleRate = sampleRate;
            this.intervalLength = intervalLength;
            inputChannelCount = inputData.Length;
            outputChannelCount = outputData.Length;
            dataLength = inputData[0].Length;

            realtimeDsps = new List<IRealtimeDsp>();
            nonrealtimeDsps = new List<INonrealtimeDsp>();

            processedSampleCount = 0;

            state = DspState.Initialized;
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
                var context = new MemoryDspContext(this);
                return context;
            }
            else
            {
                throw new InvalidOperationException("Run method must be called when DSP is not running.");
            }
        }

        public void Dispose()
        {
            if (state == DspState.Disposed)
            {
                return;
            }

            state = DspState.Disposed;
        }

        private void CheckDisposed()
        {
            if (state == DspState.Disposed)
            {
                throw new ObjectDisposedException(nameof(FileDspDriver));
            }
        }

        public string GetInputChannelName(int channel)
        {
            return "Input " + (channel + 1);
        }

        public string GetOutputChannelName(int channel)
        {
            return "Output " + (channel + 1);
        }

        public string DriverName
        {
            get
            {
                return nameof(MemoryDspDriver);
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
                return inputChannelCount;
            }
        }

        public int OutputChannelCount
        {
            get
            {
                return outputChannelCount;
            }
        }

        public int IntervalLength
        {
            get
            {
                return intervalLength;
            }
        }



        private class MemoryDspContext : IDspContext
        {
            private readonly MemoryDspDriver driver;

            private DspBufferEntry entry;
            private List<IDspCommand> commandOutputBuffer;
            private List<IDspCommand> commandInputBuffer;

            private Task completion;

            public MemoryDspContext(MemoryDspDriver driver)
            {
                this.driver = driver;

                entry = new DspBufferEntry(driver.inputChannelCount, driver.outputChannelCount, driver.intervalLength);
                commandOutputBuffer = new List<IDspCommand>();
                commandInputBuffer = new List<IDspCommand>();

                driver.state = DspState.Running;

                completion = Task.Run((Action)Run).ContinueWith(task => driver.state = DspState.Stop);
            }

            private void Run()
            {
                var currentPosition = 0;

                while (true)
                {
                    var restLength = driver.dataLength - currentPosition;
                    if (restLength == 0)
                    {
                        break;
                    }

                    var readLength = Math.Min(driver.intervalLength, restLength);
                    for (var ch = 0; ch < driver.inputChannelCount; ch++)
                    {
                        Array.Copy(driver.inputData[ch], currentPosition, entry.InputInterval[ch], 0, readLength);
                    }

                    var value = 0;
                    foreach (var dsp in driver.realtimeDsps)
                    {
                        value = dsp.Process(entry.InputInterval, entry.OutputInterval, readLength);
                    }
                    entry.RealtimeDspReturnValue = value;
                    if (driver.outputData != null)
                    {
                        for (var ch = 0; ch < driver.outputChannelCount; ch++)
                        {
                            Array.Copy(entry.OutputInterval[ch], 0, driver.outputData[ch], currentPosition, readLength);
                        }
                    }

                    var intervalCommand = new IntervalCommand(entry, readLength);
                    Post(intervalCommand);
                    while (commandOutputBuffer.Count > 0)
                    {
                        commandInputBuffer.AddRange(commandOutputBuffer);
                        commandOutputBuffer.Clear();
                        foreach (var command in commandInputBuffer)
                        {
                            foreach (var dsp in driver.nonrealtimeDsps)
                            {
                                dsp.Process(this, command);
                            }
                        }
                        commandInputBuffer.Clear();
                    }

                    currentPosition += readLength;

                    driver.processedSampleCount += readLength;
                }
            }

            public void Post(IDspCommand command)
            {
                commandOutputBuffer.Add(command);
            }

            public long ProcessedSampleCount
            {
                get
                {
                    return driver.processedSampleCount;
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



        private enum DspState
        {
            Initialized = 1,
            Running = 2,
            Stop = 3,
            Disposed = 4
        }
    }
}
