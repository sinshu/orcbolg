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
        private readonly string filename;
        private readonly int intervalLength;

        private readonly List<IRealtimeDsp> realtimeDsps;
        private readonly List<INonrealtimeDsp> nonrealtimeDsps;

        private WaveFileReader reader;

        public FileDspDriver(string filename, int intervalLength)
        {
            this.filename = filename;
            this.intervalLength = intervalLength;

            realtimeDsps = new List<IRealtimeDsp>();
            nonrealtimeDsps = new List<INonrealtimeDsp>();

            reader = new WaveFileReader(filename);
        }

        public string DriverName => filename;
        public int SampleRate => reader.WaveFormat.SampleRate;
        public int InputChannelCount => reader.WaveFormat.Channels;
        public int OutputChannelCount => 0;

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
            context.Completion = Task.Run(() => Go(context));
            //context.Completion = Go(context);
            return context;
        }

        private async Task Go(Context context)
        {
            var entry = new DspBufferEntry(reader.WaveFormat.Channels, 0, intervalLength);
            var sourceLength = reader.Length / reader.BlockAlign;
            var count = 0;
            for (var t = 0; t < sourceLength; t++)
            {
                var frame = reader.ReadNextSampleFrame();
                for (var ch = 0; ch < reader.WaveFormat.Channels; ch++)
                {
                    entry.InputInterval[ch][count] = frame[ch];
                }
                count++;
                if (count == intervalLength)
                {
                    foreach (var dsp in realtimeDsps)
                    {
                        dsp.Process(entry.InputInterval, entry.OutputInterval, intervalLength);
                    }
                    var intervalCommand = new IntervalCommand(entry, intervalLength);
                    context.Post(intervalCommand);
                    count = 0;
                    //await Task.Delay(1);
                }
            }
        }

        private class Context : IDspContext
        {
            private readonly FileDspDriver driver;

            public Context(FileDspDriver driver)
            {
                this.driver = driver;
            }

            public void Post(IDspCommand command)
            {
                foreach (var dsp in driver.nonrealtimeDsps)
                {
                    dsp.Process(this, command);
                }
            }

            public long ProcessedSampleCount => 0;

            public Task Completion { get; set; }
        }
    }
}
