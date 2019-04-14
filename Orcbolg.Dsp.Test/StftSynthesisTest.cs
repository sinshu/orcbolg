using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Orcbolg.Dsp.Test
{
    [TestClass]
    public class StftSynthesisTest
    {
        [TestMethod]
        public void Test1()
        {
            var seed = 2357;
            var testCases = Utilities.ReadTestCases(@"cases\StftSynthesisTest.Test1.csv");
            foreach (var testCase in testCases)
            {
                var dataLength = testCase["dataLength"];
                var intervalLength = testCase["intervalLength"];
                var channelCount = testCase["channelCount"];
                var frameLength = testCase["frameLength"];
                var frameShift = testCase["frameShift"];

                var random = new Random(seed);
                var inputData = Enumerable
                    .Range(0, channelCount)
                    .Select(ch => Enumerable.Range(0, dataLength).Select(t => (float)(random.NextDouble() - 0.5)).ToArray())
                    .ToArray();
                var outputData = Enumerable
                    .Range(0, channelCount)
                    .Select(ch => new float[dataLength])
                    .ToArray();

                using (var driver = new MemoryDspDriver(inputData, outputData, 16000, intervalLength))
                {
                    driver.AddDsp(new TestDsp1(driver, frameLength, frameShift));
                    driver.Run().Completion.Wait();
                }

                var expected = inputData
                    .Select(x => EnumerableEx.Repeat(0F, frameLength).Concat(x.Take(dataLength - frameLength)).ToArray())
                    .ToArray();
                Utilities.AreEqual(expected, outputData);
            }
        }

        [TestMethod]
        public void Test2()
        {
            var seed = 2357;
            var testCases = Utilities.ReadTestCases(@"cases\StftSynthesisTest.Test2.csv");
            foreach (var testCase in testCases)
            {
                var dataLength = testCase["dataLength"];
                var intervalLength = testCase["intervalLength"];
                var channelCount = testCase["channelCount"];
                var frameLength = testCase["frameLength"];
                var frameShift = testCase["frameShift"];

                var random = new Random(seed);
                var inputData = Enumerable
                    .Range(0, channelCount)
                    .Select(ch => Enumerable.Range(0, dataLength).Select(t => (float)(random.NextDouble() - 0.5)).ToArray())
                    .ToArray();
                var outputData = Enumerable
                    .Range(0, 1)
                    .Select(ch => new float[dataLength])
                    .ToArray();

                using (var driver = new MemoryDspDriver(inputData, outputData, 16000, intervalLength))
                {
                    driver.AddDsp(new TestDsp2(driver, frameLength, frameShift));
                    driver.Run().Completion.Wait();
                }

                var shifted = inputData
                    .Select(x => EnumerableEx.Repeat(0F, frameLength).Concat(x.Take(dataLength - frameLength)).ToArray())
                    .ToArray();
                var expected = Enumerable
                    .Range(0, dataLength)
                    .Select(t => Enumerable.Range(0, channelCount).Select(ch => shifted[ch][t]).Sum())
                    .ToArray();
                Utilities.AreEqual(expected, outputData[0]);
            }
        }

        private class TestDsp1 : IRealtimeDsp
        {
            private IDspDriver driver;
            private int frameLength;
            private StftSynthesis stftSynthesis;

            public TestDsp1(IDspDriver driver, int frameLength, int frameShift)
            {
                this.driver = driver;
                this.frameLength = frameLength;
                stftSynthesis = new StftSynthesis(driver.InputChannelCount, driver.OutputChannelCount, WindowFunc.CreateHann(frameLength), frameShift, StftFunc);
            }

            public int Process(float[][] inputInterval, float[][] outputInterval, int length)
            {
                stftSynthesis.Process(inputInterval, outputInterval, length);
                return 0;
            }

            public void StftFunc(long position, Complex[][] inputStft, Complex[][] outputStft)
            {
                for (var ch = 0; ch < inputStft.Length; ch++)
                {
                    Array.Copy(inputStft[ch], outputStft[ch], frameLength);
                }
            }
        }

        private class TestDsp2 : IRealtimeDsp
        {
            private IDspDriver driver;
            private int frameLength;
            private StftSynthesis stftSynthesis;

            public TestDsp2(IDspDriver driver, int frameLength, int frameShift)
            {
                this.driver = driver;
                this.frameLength = frameLength;
                stftSynthesis = new StftSynthesis(driver.InputChannelCount, driver.OutputChannelCount, WindowFunc.CreateHann(frameLength), frameShift, StftFunc);
            }

            public int Process(float[][] inputInterval, float[][] outputInterval, int length)
            {
                stftSynthesis.Process(inputInterval, outputInterval, length);
                return 0;
            }

            public void StftFunc(long position, Complex[][] inputStft, Complex[][] outputStft)
            {
                Array.Clear(outputStft[0], 0, frameLength);
                for (var ch = 0; ch < inputStft.Length; ch++)
                {
                    for (var w = 0; w < frameLength; w++)
                    {
                        outputStft[0][w] += inputStft[ch][w];
                    }
                }
            }
        }
    }
}
