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
    public class FeatureExtractionTest
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
                FeatureExtraction.GetNormalizedAmplitude(buffer, amplitude);
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
                FeatureExtraction.GetNormalizedAmplitude(buffer, amplitude, scale);
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
        public void GetAmplitudeWithTriangularFilter1()
        {
            var value = 3.1;
            var length = 1024;
            var source = EnumerableEx.Repeat(value, length).ToArray();
            var random = new Random(2357);
            for (var i = 0; i < 100; i++)
            {
                var indices = Enumerable.Range(0, 3).Select(x => length * random.NextDouble()).ToArray();
                Array.Sort(indices);
                var actual = FeatureExtraction.GetAmplitudeWithTriangularFilter(source, indices[0], indices[1], indices[2]);
                Assert.AreEqual(value, actual, 0.001);
            }
        }

        [TestMethod]
        public void GetAmplitudeWithTriangularFilter2()
        {
            var value = 3.1;
            var length = 1024;
            var source = EnumerableEx.Repeat(value, length).ToArray();
            var random = new Random(2357);
            for (var i = 0; i < length - 1; i++)
            {
                var actual = FeatureExtraction.GetAmplitudeWithTriangularFilter(source, i, i + 1, i + 2);
                Assert.AreEqual(value, actual, 0.001);
            }
        }

        [TestMethod]
        public void GetAmplitudeWithTriangularFilter3()
        {
            var value = 3.1;
            var length = 1024;
            var source = EnumerableEx.Repeat(value, length).ToArray();
            var random = new Random(2357);
            for (var i = 0; i < length; i++)
            {
                var actual = FeatureExtraction.GetAmplitudeWithTriangularFilter(source, i, i, i);
                Assert.AreEqual(value, actual, 0.001);
            }
        }
    }
}
