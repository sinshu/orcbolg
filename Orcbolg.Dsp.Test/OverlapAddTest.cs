using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Orcbolg.Dsp.Test
{
    [TestClass]
    public class OverlapAddTest
    {
        [TestMethod]
        public void Test()
        {
            var seed = 2357;
            var testCases = Utilities.ReadTestCases(@"cases\OverlapAddTest.Test.csv");
            foreach (var testCase in testCases)
            {
                var dataLength = testCase["dataLength"];
                var intervalLength = testCase["intervalLength"];
                var channelCount = testCase["channelCount"];
                var frameLength = testCase["frameLength"];
                var frameShift = testCase["frameShift"];

                var random = new Random(seed);
                var data = Enumerable
                    .Range(0, channelCount)
                    .Select(ch => Enumerable.Range(0, dataLength).Select(t => (float)(random.NextDouble() - 0.5)).ToArray())
                    .ToArray();

                var buffered = data
                    .Select(x => EnumerableEx.Repeat(0F, frameLength - frameShift).Concat(x))
                    .Select(x => x.Buffer(frameLength, frameShift).Where(b => b.Count == frameLength).Select(b => b.ToArray()).ToArray())
                    .ToArray();
                var factor = Enumerable
                    .Range(0, channelCount)
                    .Select(ch => Enumerable.Range(0, buffered[0].Length).Select(i => Enumerable.Range(0, frameLength).Select(t => (float)(random.NextDouble() - 0.5)).ToArray()).ToArray())
                    .ToArray();

                var outputBuffer = Enumerable
                    .Range(0, channelCount)
                    .Select(ch => new float[dataLength])
                    .ToArray();
                using (var driver = new MemoryDspDriver(data, outputBuffer, 16000, intervalLength))
                {
                    driver.AddDsp(new TestDsp(driver, frameLength, frameShift, factor));
                    driver.Run().Completion.Wait();
                }

                var expected = Enumerable
                    .Range(0, channelCount)
                    .Select(ch => new float[dataLength + frameLength])
                    .ToArray();
                for (var i = 0; i < buffered[0].Length; i++)
                {
                    var start = frameShift * (i + 1);
                    for (var t = 0; t < frameLength; t++)
                    {
                        for (var ch = 0; ch < channelCount; ch++)
                        {
                            expected[ch][start + t] += factor[ch][i][t] * buffered[ch][i][t];
                        }
                    }
                }
                expected = expected
                    .Select(x => x.Take(dataLength).ToArray())
                    .ToArray();
                Utilities.AreEqual(expected, outputBuffer);
            }
        }

        private class TestDsp : IRealtimeDsp
        {
            private IDspDriver driver;
            private int frameLength;
            private int frameShift;
            private float[][][] factor;
            private OverlapAdd overlapAdd;
            private int frameCount;

            public TestDsp(IDspDriver driver, int frameLength, int frameShift, float[][][] factor)
            {
                this.driver = driver;
                this.frameLength = frameLength;
                this.frameShift = frameShift;
                this.factor = factor;
                overlapAdd = new OverlapAdd(driver.InputChannelCount, driver.OutputChannelCount, frameLength, frameShift, FrameFunc);
                frameCount = 0;
            }

            public int Process(float[][] inputInterval, float[][] outputInterval, int length)
            {
                overlapAdd.Process(inputInterval, outputInterval, length);
                return 0;
            }

            public void FrameFunc(long position, float[][] inputFrame, float[][] outputFrame)
            {
                for (var ch = 0; ch < outputFrame.Length; ch++)
                {
                    for (var t = 0; t < frameLength; t++)
                    {
                        outputFrame[ch][t] = factor[ch][frameCount][t] * inputFrame[ch][t];
                    }
                }
                frameCount++;
            }
        }
    }
}
