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
        private byte[] buffer;

        public RecordTest(IDspDriver driver)
        {
            this.driver = driver;
            var format = new WaveFormat(driver.SampleRate, driver.InputChannelCount);
            writer = new WaveFileWriter("test.wav", format);
            buffer = new byte[format.BlockAlign * driver.IntervalLength];
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
                DspHelper.WriteInt16(interval.InputInterval, buffer, interval.Length);
                writer.Write(buffer, 0, writer.WaveFormat.BlockAlign * interval.Length);
            }
        }
    }
}
