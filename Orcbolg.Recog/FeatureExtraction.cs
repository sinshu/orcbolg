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
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            var half = source.Length / 2;
            if (destination.Length != half) throw new ArgumentException("Length of destination must be half the length of source.");

            destination[0] = source[0].Magnitude / source.Length;
            for (var w = 1; w < destination.Length; w++)
            {
                destination[w] = source[w].Magnitude / half;
            }
        }
    }
}
