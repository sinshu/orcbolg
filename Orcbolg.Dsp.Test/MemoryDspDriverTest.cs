using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Orcbolg.Dsp.Test
{
    [TestClass]
    public class MemoryDspDriverTest
    {
        [TestMethod]
        public void Read1()
        {
            var seed = 2357;
            var testCases = Utilities.ReadTestCases(@"cases\MemoryDspDriverTest.Read1.csv");
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
                var reference = data
                    .Select(x => x.Repeat(repeat).ToArray())
                    .ToArray();

                TestDsp testDsp;
                IDspContext context = null;
                try
                {
                    using (var driver = new MemoryDspDriver(data, 16000, intervalLength))
                    {
                        testDsp = new TestDsp(driver, reference);
                        driver.AddDsp(testDsp);
                        for (var i = 0; i < repeat; i++)
                        {
                            context = driver.Run();
                            context.Completion.Wait();
                        }
                    }
                }
                catch
                {
                    Assert.IsTrue(error == 1);
                    continue;
                }
                Assert.IsTrue(testDsp.ProcessedSampleCount == context.ProcessedSampleCount);

                Assert.IsTrue(error == 0);
            }
        }

        [TestMethod]
        public void Read2()
        {
            var seed = 2357;
            var testCases = Utilities.ReadTestCases(@"cases\MemoryDspDriverTest.Read2.csv");
            foreach (var testCase in testCases)
            {
                var repeat = testCase["repeat"];
                var channelCount = testCase["channelCount"];
                var dataLength = testCase["dataLength"];
                var intervalLength = testCase["intervalLength"];
                var sampleOffset = testCase["sampleOffset"];
                var sampleCount = testCase["sampleCount"];
                var error = testCase["error"];

                var random = new Random(seed);
                var data = Enumerable
                    .Range(0, channelCount)
                    .Select(ch => Enumerable.Range(0, dataLength).Select(t => (float)(random.NextDouble() - 0.5)).ToArray())
                    .ToArray();
                var reference = data
                    .Select(x => x.Skip(sampleOffset).Take(sampleCount).Repeat(repeat).ToArray())
                    .ToArray();

                TestDsp testDsp;
                IDspContext context = null;
                try
                {
                    using (var driver = new MemoryDspDriver(data, 16000, intervalLength))
                    {
                        driver.SetSpan(sampleOffset, sampleCount);
                        testDsp = new TestDsp(driver, reference);
                        driver.AddDsp(testDsp);
                        for (var i = 0; i < repeat; i++)
                        {
                            context = driver.Run();
                            context.Completion.Wait();
                        }
                    }
                }
                catch
                {
                    Assert.IsTrue(error == 1);
                    continue;
                }
                Assert.IsTrue(testDsp.ProcessedSampleCount == context.ProcessedSampleCount);

                Assert.IsTrue(error == 0);
            }
        }

        [TestMethod]
        public void Write1()
        {
            var seed = 2357;
            var testCases = Utilities.ReadTestCases(@"cases\MemoryDspDriverTest.Write1.csv");
            var gain = 1.5F;
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
                var reference = data
                    .Select(x => x.Repeat(repeat).ToArray())
                    .ToArray();
                var outputBuffer = Enumerable
                    .Range(0, channelCount)
                    .Select(ch => new float[dataLength])
                    .ToArray();

                TestDsp testDsp;
                IDspContext context = null;
                try
                {
                    using (var driver = new MemoryDspDriver(data, outputBuffer, 16000, intervalLength))
                    {
                        testDsp = new TestDsp(driver, reference);
                        driver.AddDsp(testDsp);

                        var bypass = new Bypass(driver);
                        Enumerable.Range(0, channelCount).ForEach(ch => bypass.SetConnection(ch, ch));
                        driver.AddDsp(bypass);

                        var outputGain = new OutputGain(driver, EnumerableEx.Repeat(gain, channelCount).ToArray());
                        driver.AddDsp(outputGain);

                        for (var i = 0; i < repeat; i++)
                        {
                            context = driver.Run();
                            context.Completion.Wait();
                        }
                    }
                }
                catch
                {
                    Assert.IsTrue(error == 1);
                    continue;
                }
                Assert.IsTrue(testDsp.ProcessedSampleCount == context.ProcessedSampleCount);

                Utilities.AreEqual(data.Select(x => x.Select(s => gain * s).ToArray()).ToArray(), outputBuffer);

                Assert.IsTrue(error == 0);
            }
        }

        [TestMethod]
        public void Write2()
        {
            var seed = 2357;
            var testCases = Utilities.ReadTestCases(@"cases\MemoryDspDriverTest.Write2.csv");
            foreach (var testCase in testCases)
            {
                var repeat = testCase["repeat"];
                var channelCount = testCase["channelCount"];
                var dataLength = testCase["dataLength"];
                var intervalLength = testCase["intervalLength"];
                var sampleOffset = testCase["sampleOffset"];
                var sampleCount = testCase["sampleCount"];
                var error = testCase["error"];

                var random = new Random(seed);
                var data = Enumerable
                    .Range(0, channelCount)
                    .Select(ch => Enumerable.Range(0, dataLength).Select(t => (float)(random.NextDouble() - 0.5)).ToArray())
                    .ToArray();
                var reference = data
                    .Select(x => x.Skip(sampleOffset).Take(sampleCount).Repeat(repeat).ToArray())
                    .ToArray();
                var outputBuffer = Enumerable
                    .Range(0, channelCount)
                    .Select(ch => new float[sampleCount])
                    .ToArray();

                TestDsp testDsp;
                IDspContext context = null;
                try
                {
                    using (var driver = new MemoryDspDriver(data, outputBuffer, 16000, intervalLength))
                    {
                        Console.WriteLine("IL: " + intervalLength);
                        driver.SetSpan(sampleOffset, sampleCount);

                        testDsp = new TestDsp(driver, reference);
                        driver.AddDsp(testDsp);

                        var bypass = new Bypass(driver);
                        Enumerable.Range(0, channelCount).ForEach(ch => bypass.SetConnection(ch, ch));
                        driver.AddDsp(bypass);

                        for (var i = 0; i < repeat; i++)
                        {
                            context = driver.Run();
                            context.Completion.Wait();
                        }
                    }
                }
                catch
                {
                    Assert.IsTrue(error == 1);
                    continue;
                }
                Assert.IsTrue(testDsp.ProcessedSampleCount == context.ProcessedSampleCount);

                Utilities.AreEqual(data.Select(x => x.Skip(sampleOffset).Take(sampleCount).ToArray()).ToArray(), outputBuffer);

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
                Console.WriteLine(length);
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

            public int ProcessedSampleCount
            {
                get
                {
                    return processedSampleCount;
                }
            }
        }
    }
}
