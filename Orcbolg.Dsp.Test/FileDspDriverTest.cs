using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Orcbolg.Dsp.Test
{
    [TestClass]
    public class FileDspDriverTest
    {
        [TestMethod]
        public void Read1()
        {
            var seed = 2357;
            var testCases = Utilities.ReadTestCases(@"cases\FileDspDriverTest.Read1.csv");
            foreach (var testCase in testCases)
            {
                var repeat = testCase["repeat"];
                var channelCount = testCase["channelCount"];
                var dataLength = testCase["dataLength"];
                var intervalLength = testCase["intervalLength"];
                var error = testCase["error"];

                var random = new Random(seed);
                var data = Enumerable
                    .Range(0, channelCount)
                    .Select(ch => Enumerable.Range(0, dataLength).Select(t => (float)(random.NextDouble() - 0.5)).ToArray())
                    .ToArray();
                WaveFile.Write(data, 16000, "test.wav");
                var reference = data
                    .Select(x => x.Repeat(repeat).ToArray())
                    .ToArray();

                try
                {
                    using (var driver = new FileDspDriver("test.wav", intervalLength))
                    {
                        driver.AddDsp(new TestDsp(driver, reference));
                        for (var i = 0; i < repeat; i++)
                        {
                            driver.Run().Completion.Wait();
                        }
                    }
                }
                catch (Exception e)
                {
                    Assert.IsTrue(error == 1);
                    continue;
                }

                Assert.IsTrue(error == 0);
            }
        }

        private class TestDsp : IRealtimeDsp
        {
            private IDspDriver driver;
            private float[][] reference;
            private int processedSampleCount;

            public TestDsp(IDspDriver driver, float[][] reference)
            {
                this.driver = driver;
                this.reference = reference;
                processedSampleCount = 0;
            }

            public int Process(float[][] inputInterval, float[][] outputInterval, int length)
            {
                var expected = reference
                    .Select(x => x.Skip(processedSampleCount).Take(length).ToArray())
                    .ToArray();
                var actual = inputInterval
                    .Select(x => x.Take(length).ToArray())
                    .ToArray();
                Utilities.AreEqual(expected, actual);

                processedSampleCount += length;
                return 0;
            }
        }
    }
}
