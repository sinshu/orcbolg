using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;

namespace Orcbolg.Dsp
{
    public class RecordTest : INonrealtimeDsp, IDisposable
    {
        private IDspDriver driver;
        private WaveFileWriter writer;

        public RecordTest(IDspDriver driver)
        {
            this.driver = driver;
            var format = new WaveFormat(driver.SampleRate, driver.InputChannelCount);
            writer = new WaveFileWriter("test.wav", format);
        }

        public void Dispose()
        {
            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }
        }

        public void Process(IDspContext contect, IDspCommand command)
        {
            var interval = command as IntervalCommand;
            if (interval != null)
            {
                for (var t = 0; t < interval.Length; t++)
                {
                    for (var ch = 0; ch < interval.InputInterval.Length; ch++)
                    {
                        writer.WriteSample(interval.InputInterval[ch][t]);
                    }
                }
            }
        }
    }
}
