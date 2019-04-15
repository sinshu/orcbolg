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
        public void GetNormalizedAmplitude()
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
    }
}
