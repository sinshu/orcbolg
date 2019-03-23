using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private readonly int inputChannelCount;
        private readonly int outputChannelCount;

        public FileDspDriver(string inputFileName, int intervalLength)
        {
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

                this.inputChannelCount = reader.WaveFormat.Channels;
                this.outputChannelCount = 0;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public FileDspDriver(string inputFileName, string outputFileName, int outputChannelCount, int intervalLength)
        {
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

                this.inputChannelCount = reader.WaveFormat.Channels;
                this.outputChannelCount = outputChannelCount;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void AddDsp(IRealtimeDsp dsp)
        {
            realtimeDsps.Add(dsp);
        }

        public void AddDsp(INonrealtimeDsp dsp)
        {
            nonrealtimeDsps.Add(dsp);
        }

        public IDspContext Run()
        {
            var context = new Context(this);
            return context;
        }

        public void Dispose()
        {
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
                return reader.WaveFormat.SampleRate;
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



        private class Context : IDspContext
        {
            private readonly FileDspDriver driver;

            private DspBufferEntry entry;
            private List<IDspCommand> commandOutputBuffer;
            private List<IDspCommand> commandInputBuffer;
            private long processedSampleCount;

            private Task completion;

            public Context(FileDspDriver driver)
            {
                this.driver = driver;

                entry = new DspBufferEntry(driver.inputChannelCount, driver.outputChannelCount, driver.intervalLength);
                commandOutputBuffer = new List<IDspCommand>();
                commandInputBuffer = new List<IDspCommand>();
                processedSampleCount = 0;

                completion = Task.Run((Action)Run);
            }

            private void Run()
            {
                while (true)
                {
                    var read = DspHelper.FillBuffer(driver.reader, driver.readBuffer);
                    var sampleCount = read / driver.reader.WaveFormat.BlockAlign;
                    if (sampleCount == 0)
                    {
                        return;
                    }

                    DspHelper.ReadInt16(driver.readBuffer, entry.InputInterval, sampleCount);
                    foreach (var dsp in driver.realtimeDsps)
                    {
                        dsp.Process(entry.InputInterval, entry.OutputInterval, sampleCount);
                    }
                    if (driver.writer != null)
                    {
                        DspHelper.WriteInt16(entry.OutputInterval, driver.writeBuffer, sampleCount);
                        driver.writer.Write(driver.writeBuffer, 0, driver.writer.WaveFormat.BlockAlign * sampleCount);
                    }

                    var intervalCommand = new IntervalCommand(entry, sampleCount);
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

                    processedSampleCount += sampleCount;

                    if (sampleCount < driver.intervalLength)
                    {
                        return;
                    }
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
    }
}
