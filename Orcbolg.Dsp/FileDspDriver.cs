using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Orcbolg.Dsp
{
    public sealed class FileDspDriver : IDspDriver
    {
        private readonly string inputFileName;
        private readonly string outputFileName;
        private readonly int intervalLength;

        private readonly List<IRealtimeDsp> realtimeDsps;
        private readonly List<INonrealtimeDsp> nonrealtimeDsps;

        private WaveFileReader reader;
        private byte[] readBuffer;
        private WaveFileWriter writer;
        private byte[] writeBuffer;
        private int startPosition;
        private int processLength;

        private readonly int sampleRate;
        private readonly int inputChannelCount;
        private readonly int outputChannelCount;

        private DspState state;

        public FileDspDriver(string inputFileName, int intervalLength)
        {
            if (inputFileName == null) throw new ArgumentNullException(nameof(inputFileName));
            if (intervalLength <= 0) throw new ArgumentException("Interval length must be equal to or greater than 1.");

            try
            {
                this.inputFileName = inputFileName;
                this.outputFileName = null;
                this.intervalLength = intervalLength;

                realtimeDsps = new List<IRealtimeDsp>();
                nonrealtimeDsps = new List<INonrealtimeDsp>();

                reader = new WaveFileReader(inputFileName);
                readBuffer = new byte[reader.WaveFormat.BlockAlign * intervalLength];
                writer = null;
                writeBuffer = null;
                startPosition = 0;
                processLength = (int)(reader.Length / reader.WaveFormat.BlockAlign);

                this.sampleRate = reader.WaveFormat.SampleRate;
                this.inputChannelCount = reader.WaveFormat.Channels;
                this.outputChannelCount = 0;

                state = DspState.Initialized;
            }
            catch (Exception e)
            {
                Dispose();
                ExceptionDispatchInfo.Capture(e).Throw();
            }
        }

        public FileDspDriver(string inputFileName, string outputFileName, int outputChannelCount, int intervalLength)
        {
            if (inputFileName == null) throw new ArgumentNullException(nameof(inputFileName));
            if (outputFileName == null) throw new ArgumentNullException(nameof(outputFileName));
            if (outputChannelCount <= 0) throw new ArgumentException("At least one output channel must be specified.");
            if (intervalLength <= 0) throw new ArgumentException("Interval length must be equal to or greater than 1.");

            try
            {
                this.inputFileName = inputFileName;
                this.outputFileName = outputFileName;
                this.intervalLength = intervalLength;

                realtimeDsps = new List<IRealtimeDsp>();
                nonrealtimeDsps = new List<INonrealtimeDsp>();

                reader = new WaveFileReader(inputFileName);
                readBuffer = new byte[reader.WaveFormat.BlockAlign * intervalLength];
                writer = new WaveFileWriter(outputFileName, new WaveFormat(reader.WaveFormat.SampleRate, 16, outputChannelCount));
                writeBuffer = new byte[writer.WaveFormat.BlockAlign * intervalLength];
                startPosition = 0;
                processLength = (int)(reader.Length / reader.WaveFormat.BlockAlign);

                this.sampleRate = reader.WaveFormat.SampleRate;
                this.inputChannelCount = reader.WaveFormat.Channels;
                this.outputChannelCount = outputChannelCount;

                state = DspState.Initialized;
            }
            catch (Exception e)
            {
                Dispose();
                ExceptionDispatchInfo.Capture(e).Throw();
            }
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

        public void SetStartPosition(int position)
        {
            CheckDisposed();

            if (state == DspState.Running)
            {
                throw new InvalidOperationException("SetStartPosition method must be called when DSP is not running.");
            }

            if (!(0 <= position && position < reader.Length / reader.BlockAlign))
            {
                throw new IndexOutOfRangeException(nameof(position));
            }

            startPosition = position;
        }

        public void SetLength(int length)
        {
            CheckDisposed();

            if (state == DspState.Running)
            {
                throw new InvalidOperationException("SetLength method must be called when DSP is not running.");
            }

            if (length <= 0)
            {
                throw new ArgumentException("Length must be equal to or greater than 1.");
            }

            processLength = length;
        }

        public IDspContext Run()
        {
            CheckDisposed();

            if (state == DspState.Initialized || state == DspState.Stop)
            {
                var context = new FileDspContext(this);
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

            if (reader != null)
            {
                reader.Dispose();
                reader = null;
            }
            if (writer != null)
            {
                writer.Dispose();
                writer = null;
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
                return inputFileName;
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



        private class FileDspContext : IDspContext
        {
            private readonly FileDspDriver driver;

            private DspBufferEntry entry;
            private List<IDspCommand> commandOutputBuffer;
            private List<IDspCommand> commandInputBuffer;
            private long processedSampleCount;

            private Task completion;

            public FileDspContext(FileDspDriver driver)
            {
                this.driver = driver;

                entry = new DspBufferEntry(driver.inputChannelCount, driver.outputChannelCount, driver.intervalLength);
                commandOutputBuffer = new List<IDspCommand>();
                commandInputBuffer = new List<IDspCommand>();
                processedSampleCount = 0;

                driver.state = DspState.Running;

                completion = Task.Run((Action)Run).ContinueWith(task => driver.state = DspState.Stop);
            }

            private void Run()
            {
                var blockAlign = driver.reader.WaveFormat.BlockAlign;

                var startPosition = driver.startPosition;
                var endPosition = startPosition + driver.processLength;
                var sourceLength = (int)(driver.reader.Length / blockAlign);
                if (endPosition > sourceLength)
                {
                    endPosition = sourceLength;
                }
                var processLength = endPosition - startPosition;

                var currentPosition = 0;

                driver.reader.Seek(blockAlign * startPosition, SeekOrigin.Begin);

                while (true)
                {
                    var restLength = processLength - currentPosition;
                    if (restLength == 0)
                    {
                        break;
                    }

                    var readLength = Math.Min(driver.intervalLength, restLength);
                    DspHelper.FillBuffer(driver.reader, driver.readBuffer, blockAlign * readLength);
                    DspHelper.ReadInt16(driver.readBuffer, entry.InputInterval, readLength);

                    var value = 0;
                    foreach (var dsp in driver.realtimeDsps)
                    {
                        value = dsp.Process(entry.InputInterval, entry.OutputInterval, readLength);
                    }
                    entry.RealtimeDspReturnValue = value;
                    if (driver.writer != null)
                    {
                        DspHelper.WriteInt16(entry.OutputInterval, driver.writeBuffer, readLength);
                        driver.writer.Write(driver.writeBuffer, 0, driver.writer.WaveFormat.BlockAlign * readLength);
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

                    processedSampleCount += readLength;
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
                    return processedSampleCount;
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
