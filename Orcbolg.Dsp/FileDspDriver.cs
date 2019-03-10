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
        private readonly int intervalLength;
        private readonly string outputFileName;
        private readonly int outputChannelCount;

        private readonly List<IRealtimeDsp> realtimeDsps;
        private readonly List<INonrealtimeDsp> nonrealtimeDsps;

        private WaveFileReader reader;
        private byte[] readBuffer;
        private WaveFileWriter writer;
        private byte[] writeBuffer;

        private readonly DspBufferEntry entry;

        public FileDspDriver(string inputFileName, int intervalLength)
        {
            this.inputFileName = inputFileName;
            this.intervalLength = intervalLength;
            outputFileName = null;
            outputChannelCount = 0;

            realtimeDsps = new List<IRealtimeDsp>();
            nonrealtimeDsps = new List<INonrealtimeDsp>();

            reader = new WaveFileReader(inputFileName);
            readBuffer = new byte[reader.WaveFormat.BlockAlign * intervalLength];
            writer = null;
            writeBuffer = null;

            entry = new DspBufferEntry(reader.WaveFormat.Channels, 0, intervalLength);
        }

        public FileDspDriver(string inputFileName, int intervalLength, string outputFileName, int outputChannelCount)
        {
            this.inputFileName = inputFileName;
            this.intervalLength = intervalLength;
            this.outputFileName = outputFileName;
            this.outputChannelCount = outputChannelCount;

            realtimeDsps = new List<IRealtimeDsp>();
            nonrealtimeDsps = new List<INonrealtimeDsp>();

            reader = new WaveFileReader(inputFileName);
            readBuffer = new byte[reader.WaveFormat.BlockAlign * intervalLength];
            writer = new WaveFileWriter(outputFileName, new WaveFormat(reader.WaveFormat.SampleRate, 16, outputChannelCount));
            writeBuffer = new byte[writer.WaveFormat.BlockAlign * intervalLength];

            entry = new DspBufferEntry(reader.WaveFormat.Channels, outputChannelCount, intervalLength);
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
            context.Completion = Task.Run(() => Run(context));
            return context;
        }

        private void Run(Context context)
        {
            while (true)
            {
                var read = DspHelper.FillBuffer(reader, readBuffer);
                var sampleCount = read / reader.WaveFormat.BlockAlign;
                if (sampleCount == 0)
                {
                    return;
                }

                DspHelper.ReadInt16(readBuffer, entry.InputInterval, sampleCount);
                var command = new IntervalCommand(entry, sampleCount);

                foreach (var dsp in realtimeDsps)
                {
                    dsp.Process(command.InputInterval, command.OutputInterval, sampleCount);
                }

                if (writer != null)
                {
                    DspHelper.WriteInt16(command.OutputInterval, writeBuffer, sampleCount);
                    writer.Write(writeBuffer, 0, writer.WaveFormat.BlockAlign * sampleCount);
                }

                context.Post(command);
                if (sampleCount < intervalLength)
                {
                    return;
                }
            }
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

        public string DriverName => inputFileName;
        public int SampleRate => reader.WaveFormat.SampleRate;
        public int InputChannelCount => reader.WaveFormat.Channels;
        public int OutputChannelCount => outputChannelCount;
        public int IntervalLength => intervalLength;



        private class Context : IDspContext
        {
            private readonly FileDspDriver driver;

            private long processedSampleCount;

            public Context(FileDspDriver driver)
            {
                this.driver = driver;

                processedSampleCount = 0;
            }

            public void Post(IDspCommand command)
            {
                foreach (var dsp in driver.nonrealtimeDsps)
                {
                    dsp.Process(this, command);
                }

                var intervalCommand = command as IntervalCommand;
                if (intervalCommand != null)
                {
                    processedSampleCount += intervalCommand.Length;
                }
            }

            public long ProcessedSampleCount => processedSampleCount;
            public Task Completion { get; set; }
        }
    }
}
