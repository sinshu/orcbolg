using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Orcbolg.Dsp.Test
{
    [TestClass]
    public class StftAnalysisTest
    {
        [TestMethod]
        public void Test()
        {
            var seed = 2357;
            var testCases = Utilities.ReadTestCases(@"cases\StftAnalysisTest.Test.csv");
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
                var reference = Enumerable
                    .Range(0, buffered[0].Length)
                    .Select(i => Enumerable.Range(0, channelCount).Select(ch => buffered[ch][i]).ToArray())
                    .ToArray();

                using (var driver = new MemoryDspDriver(data, 16000, intervalLength))
                {
                    driver.AddDsp(new TestDsp(driver, frameLength, frameShift, reference));
                    driver.Run().Completion.Wait();
                }
            }
        }

        private class TestDsp : INonrealtimeDsp
        {
            private IDspDriver driver;
            private int frameLength;
            private int frameShift;
            private float[][][] reference;
            private double[] window;
            private StftAnalysis stftAnalysis;
            private int frameCount;
            private int expectedPosition;

            public TestDsp(IDspDriver driver, int frameLength, int frameShift, float[][][] reference)
            {
                this.driver = driver;
                this.reference = reference;
                this.frameLength = frameLength;
                this.frameShift = frameShift;
                window = WindowFunc.CreateHann(frameLength);
                stftAnalysis = new StftAnalysis(driver.InputChannelCount, window, frameShift, StftAction);
                frameCount = 0;
                expectedPosition = frameShift - frameLength;
            }

            public void Process(IDspContext context, IDspCommand command)
            {
                var intervalCommand = command as IntervalCommand;
                if (intervalCommand != null)
                {
                    stftAnalysis.Process(context, intervalCommand.InputInterval, intervalCommand.Length);
                }
            }

            public void StftAction(IDspContext context, long position, Complex[][] stft)
            {
                var expected = reference[frameCount].Select(x => x.Zip(window, (c1, c2) => (float)(c1 * c2)).ToArray()).ToArray();
                stft.ForEach(x => Fourier.Inverse(x, FourierOptions.AsymmetricScaling));
                var actual = stft.Select(x => x.Select(c => (float)c.Real).ToArray()).ToArray();
                Utilities.AreEqual(expected, actual);
                Assert.IsTrue(expectedPosition == position);
                expectedPosition += frameShift;
                frameCount++;
            }
        }
    }
}
