using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Orcbolg.Dsp.Test
{
    [TestClass]
    public class FramingTest
    {
        [TestMethod]
        public void Test()
        {
            var seed = 2357;
            var testCases = Utilities.ReadTestCases(@"cases\FramingTest.Test.csv");
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
            private Framing framing;
            private int frameCount;
            private int expectedPosition;

            public TestDsp(IDspDriver driver, int frameLength, int frameShift, float[][][] reference)
            {
                this.driver = driver;
                this.reference = reference;
                this.frameLength = frameLength;
                this.frameShift = frameShift;
                framing = new Framing(driver.InputChannelCount, frameLength, frameShift, FrameAction);
                frameCount = 0;
                expectedPosition = frameShift - frameLength;
            }

            public void Process(IDspContext context, IDspCommand command)
            {
                var intervalCommand = command as IntervalCommand;
                if (intervalCommand != null)
                {
                    framing.Process(context, intervalCommand.InputInterval, intervalCommand.Length);
                }
            }

            public void FrameAction(IDspContext context, long position, float[][] frame)
            {
                Utilities.AreEqual(reference[frameCount], frame);
                Assert.IsTrue(expectedPosition == position);
                expectedPosition += frameShift;
                frameCount++;
            }
        }
    }
}
