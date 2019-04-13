using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Orcbolg.Dsp.Test
{
    [TestClass]
    public class WaveFileTest
    {
        [TestMethod]
        public void ReadWrite1()
        {
            var sampleRate = 16000;
            var seed = 2357;
            var testCases = Utilities.ReadTestCases(@"cases\WaveFileTest.ReadWrite1.csv");
            foreach (var testCase in testCases)
            {
                var channelCount = testCase["channelCount"];
                var dataLength = testCase["dataLength"];
                var sampleOffset = testCase["sampleOffset"];
                var sampleCount = testCase["sampleCount"];
                var error = testCase["error"];

                var random = new Random(seed);
                var data = Enumerable
                    .Range(0, channelCount)
                    .Select(ch => Enumerable.Range(0, dataLength).Select(t => (float)(random.NextDouble() - 0.5)).ToArray())
                    .ToArray();

                try
                {
                    WaveFile.Write(data, sampleRate, "test.wav", sampleOffset, sampleCount);
                }
                catch (ArgumentException)
                {
                    Assert.IsTrue(error == 1);
                    continue;
                }

                Assert.IsTrue(error == 0);

                var expected = Enumerable
                    .Range(0, channelCount)
                    .Select(ch => data[ch].Skip(sampleOffset).Take(sampleCount).ToArray())
                    .ToArray();
                var actual = WaveFile.Read("test.wav");
                Utilities.AreEqual(expected, actual);
            }
        }

        [TestMethod]
        public void ReadWrite2()
        {
            var sampleRate = 16000;
            var seed = 2357;
            var testCases = Utilities.ReadTestCases(@"cases\WaveFileTest.ReadWrite2.csv");
            foreach (var testCase in testCases)
            {
                var channelCount = testCase["channelCount"];
                var dataLength = testCase["dataLength"];
                var sampleOffset = testCase["sampleOffset"];
                var sampleCount = testCase["sampleCount"];
                var error = testCase["error"];

                var random = new Random(seed);
                var data = Enumerable
                    .Range(0, channelCount)
                    .Select(ch => Enumerable.Range(0, dataLength).Select(t => (float)(random.NextDouble() - 0.5)).ToArray())
                    .ToArray();
                WaveFile.Write(data, sampleRate, "test.wav");

                float[][] actual;
                try
                {
                    actual = WaveFile.Read("test.wav", sampleOffset, sampleCount);
                }
                catch (ArgumentException)
                {
                    Assert.IsTrue(error == 1);
                    continue;
                }

                Assert.IsTrue(error == 0);

                var expected = Enumerable
                    .Range(0, channelCount)
                    .Select(ch => data[ch].Skip(sampleOffset).Take(sampleCount).ToArray())
                    .ToArray();
                Utilities.AreEqual(expected, actual);
            }
        }

        [TestMethod]
        public void SampleRate()
        {
            var seed = 2357;
            var random = new Random(seed);
            var channelCount = 2;
            var dataLength = 100;
            var data = Enumerable
                    .Range(0, channelCount)
                    .Select(ch => Enumerable.Range(0, dataLength).Select(t => (float)(random.NextDouble() - 0.5)).ToArray())
                    .ToArray();

            var testCases = Utilities.ReadTestCases(@"cases\WaveFileTest.SampleRate.csv");
            foreach (var testCase in testCases)
            {
                var expected = testCase["sampleRate"];
                WaveFile.Write(data, expected, "test.wav");
                int actual;
                WaveFile.Read("test.wav", out actual);
                Assert.AreEqual(expected, actual);
            }
        }
    }
}
