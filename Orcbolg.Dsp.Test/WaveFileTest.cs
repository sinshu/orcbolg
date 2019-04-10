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
            var testCases = Utilities.ReadTestCases("ReadWrite1.csv");
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
                catch
                {
                    if (error != 1)
                    {
                        Assert.Fail("result != 1");
                    }
                    continue;
                }

                if (error != 0)
                {
                    Assert.Fail("result != 0");
                }

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
            var testCases = Utilities.ReadTestCases("ReadWrite2.csv");
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
                catch
                {
                    if (error != 1)
                    {
                        Assert.Fail("result != 1");
                    }
                    continue;
                }

                if (error != 0)
                {
                    Assert.Fail("result != 0");
                }

                var expected = Enumerable
                    .Range(0, channelCount)
                    .Select(ch => data[ch].Skip(sampleOffset).Take(sampleCount).ToArray())
                    .ToArray();
                Utilities.AreEqual(expected, actual);
            }
        }
    }
}
