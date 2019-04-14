using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MathNet.Numerics;

namespace Orcbolg.Recog
{
    public static class FeatureExtraction
    {
        public static void GetNormalizedAmplitude(Complex[] source, double[] destination)
        {
            destination[0] = source[0].Magnitude / source.Length;
            var half = source.Length / 2;
            for (var w = 1; w < destination.Length; w++)
            {
                destination[w] = source[w].Magnitude / half;
            }
        }
    }
}
