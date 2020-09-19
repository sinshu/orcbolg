using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Orcbolg.Dsp.Test
{
    [TestClass]
    public class WaveRecorderTest
    {
        private static readonly string fileName = "recorded.wav";
        private static readonly int recordLength = 15000;

        [TestMethod]
        public void Record1()
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
                File.Delete(Path.GetFileNameWithoutExtension(fileName) + ".csv");
            }

            var channelCount = 3;
            var dataLength = 16000;
            var intervalLength = 57;

            var random = new Random(2357);
            var data = Enumerable
                .Range(0, channelCount)
                .Select(ch => Enumerable.Range(0, dataLength).Select(t => (float)(random.NextDouble() - 0.5)).ToArray())
                .ToArray();

            var startInterval = 3;

            using (var driver = new MemoryDspDriver(data, 16000, intervalLength))
            {
                var testDsp = new TestDsp1(driver, startInterval);
                driver.AddDsp(testDsp);

                var recorder = new WaveRecorder(driver);
                driver.AddDsp(recorder);

                var context = driver.Run();
                context.Completion.Wait();
            }

            var startSample = (startInterval + 1) * intervalLength;
            var actual = WaveFile.Read(fileName);
            var expected = data
                .Select(ch => ch.Skip(startSample).Take(recordLength).ToArray())
                .ToArray();
            Utilities.AreEqual(expected, actual);
        }

        private class TestDsp1 : INonrealtimeDsp
        {
            private IDspDriver driver;
            private int startInterval;

            private int count;

            public TestDsp1(IDspDriver driver, int startInterval)
            {
                this.driver = driver;
                this.startInterval = startInterval;

                count = 0;
            }

            public void Process(IDspContext context, IDspCommand command)
            {
                if (command is IntervalCommand)
                {
                    if (startInterval == count)
                    {
                        context.StartRecording(0, fileName, recordLength);
                    }

                    count++;
                }
            }
        }
    }
}
