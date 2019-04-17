using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orcbolg.Recog;

namespace Orcbolg.Recog.Test
{
    [TestClass]
    public class SpectralAnalysisTest
    {
        [TestMethod]
        public void GetNormalizedAmplitude1()
        {
            var frameLength = 16;
            var buffer = new Complex[frameLength];
            var amplitude = new double[frameLength / 2];
            for (var w = 0; w < frameLength / 2; w++)
            {
                for (var t = 0; t < frameLength; t++)
                {
                    buffer[t] = Math.Cos(2 * Math.PI * w * t / frameLength);
                }
                Fourier.Forward(buffer, FourierOptions.AsymmetricScaling);
                SpectralAnalysis.GetNormalizedAmplitude(buffer, amplitude);
                for (var i = 0; i < amplitude.Length; i++)
                {
                    if (i == w)
                    {
                        Assert.AreEqual(1.0, amplitude[i], 0.001);
                    }
                    else
                    {
                        Assert.AreEqual(0.0, amplitude[i], 0.001);
                    }
                }
            }
        }

        [TestMethod]
        public void GetNormalizedAmplitude2()
        {
            var scale = 3.1;
            var frameLength = 16;
            var buffer = new Complex[frameLength];
            var amplitude = new double[frameLength / 2];
            for (var w = 0; w < frameLength / 2; w++)
            {
                for (var t = 0; t < frameLength; t++)
                {
                    buffer[t] = Math.Cos(2 * Math.PI * w * t / frameLength);
                }
                Fourier.Forward(buffer, FourierOptions.AsymmetricScaling);
                SpectralAnalysis.GetNormalizedAmplitude(buffer, amplitude, scale);
                for (var i = 0; i < amplitude.Length; i++)
                {
                    if (i == w)
                    {
                        Assert.AreEqual(scale, amplitude[i], 0.001);
                    }
                    else
                    {
                        Assert.AreEqual(0.0, amplitude[i], 0.001);
                    }
                }
            }
        }

        [TestMethod]
        public void GetEnergyWithTriangularFilter1()
        {
            var value = 3.1;
            var frameLength = 1024;
            var sampleRate = 16000;
            var source = EnumerableEx.Repeat(value, frameLength / 2).ToArray();
            var random = new Random(2357);
            for (var i = 0; i < 100; i++)
            {
                var freqs = Enumerable.Range(0, 3).Select(x => sampleRate * random.NextDouble() / 2).ToArray();
                Array.Sort(freqs);
                var actual = SpectralAnalysis.GetEnergyWithTriangularFilter(source, sampleRate, frameLength, freqs[0], freqs[1], freqs[2]);
                Assert.AreEqual(value, actual, 0.001);
            }
        }

        [TestMethod]
        public void GetEnergyWithTriangularFilter2()
        {
            var frameLength = 1024;
            var sampleRate = 16000;
            var random = new Random(2357);
            var source = Enumerable.Range(0, frameLength / 2).Select(t => random.NextDouble()).ToArray();
            for (var w = 2; w < frameLength / 2; w++)
            {
                var lower = (double)(w - 2) / frameLength * sampleRate + (random.NextDouble() - 0.5) / 1000000;
                var center = (double)w / frameLength * sampleRate + (random.NextDouble() - 0.5) / 1000000; ;
                var upper = (double)(w + 1) / frameLength * sampleRate + (random.NextDouble() - 0.5) / 1000000; ;
                var expected = (0.5 * source[w - 1] + 1.0 * source[w]) / 1.5;
                var actual = SpectralAnalysis.GetEnergyWithTriangularFilter(source, sampleRate, frameLength, lower, center, upper);
                Assert.AreEqual(expected, actual, 0.001);
            }
        }

        [TestMethod]
        public void GetEnergyWithTriangularFilter3()
        {
            var frameLength = 1024;
            var sampleRate = 16000;
            var random = new Random(2357);
            var source = Enumerable.Range(0, frameLength / 2).Select(t => random.NextDouble()).ToArray();
            for (var w = 4; w < frameLength / 2 - 1; w++)
            {
                var lower = (double)(w - 4) / frameLength * sampleRate + (random.NextDouble() - 0.5) / 1000000; ;
                var center = (double)w / frameLength * sampleRate + (random.NextDouble() - 0.5) / 1000000; ;
                var upper = (double)(w + 2) / frameLength * sampleRate + (random.NextDouble() - 0.5) / 1000000; ;
                var expected = (0.25 * source[w - 3] + 0.5 * source[w - 2] + 0.75 * source[w - 1] + 1.0 * source[w] + 0.5 * source[w + 1]) / 3.0;
                var actual = SpectralAnalysis.GetEnergyWithTriangularFilter(source, sampleRate, frameLength, lower, center, upper);
                Assert.AreEqual(expected, actual, 0.001);
            }
        }

        [TestMethod]
        public void GetEnergyWithTriangularFilter4()
        {
            var frameLength = 1024;
            var sampleRate = 16000;
            var random = new Random(2357);
            var source = Enumerable.Range(0, frameLength / 2).Select(t => random.NextDouble()).ToArray();
            for (var w = 2; w < frameLength / 2 - 3; w++)
            {
                var lower = (double)(w - 2) / frameLength * sampleRate + (random.NextDouble() - 0.5) / 1000000; ;
                var center = (double)w / frameLength * sampleRate + (random.NextDouble() - 0.5) / 1000000; ;
                var upper = (double)(w + 4) / frameLength * sampleRate + (random.NextDouble() - 0.5) / 1000000; ;
                var expected = (0.5 * source[w - 1] + 1.0 * source[w] + 0.75 * source[w + 1] + 0.5 * source[w + 2] + 0.25 * source[w + 3]) / 3.0;
                var actual = SpectralAnalysis.GetEnergyWithTriangularFilter(source, sampleRate, frameLength, lower, center, upper);
                Assert.AreEqual(expected, actual, 0.001);
            }
        }

        [TestMethod]
        public void GetEnergyWithTriangularFilter5()
        {
            var frameLength = 1024;
            var sampleRate = 16000;
            var random = new Random(2357);
            var source = Enumerable.Range(0, frameLength / 2).Select(t => random.NextDouble()).ToArray();
            for (var w = 0; w < frameLength / 2 - 1; w++)
            {
                var freq = (double)w / frameLength * sampleRate + (random.NextDouble() - 0.5) / 1000000; ;
                var expected = source[w];
                var actual = SpectralAnalysis.GetEnergyWithTriangularFilter(source, sampleRate, frameLength, freq, freq, freq);
                Assert.AreEqual(expected, actual, 0.001);
            }
        }
    }
}
