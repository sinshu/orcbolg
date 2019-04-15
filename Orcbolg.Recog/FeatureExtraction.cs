using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MathNet.Numerics;

namespace Orcbolg.Recog
{
    public static class FeatureExtraction
    {
        private static readonly double epsilon = 10E-9;

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

        public static void GetNormalizedAmplitude(Complex[] source, double[] destination, double scale)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            var half = source.Length / 2;
            if (destination.Length != half) throw new ArgumentException("Length of destination must be half the length of source.");

            destination[0] = scale * source[0].Magnitude / source.Length;
            for (var w = 1; w < destination.Length; w++)
            {
                destination[w] = scale * source[w].Magnitude / half;
            }
        }

        public static double GetAmplitudeWithTriangularFilter(double[] source, double lowerIndex, double centerIndex, double upperIndex)
        {
            var num = 0.0;
            var den = 0.0;
            var count = 0;

            var leftWidth = centerIndex - lowerIndex;
            if (leftWidth >= epsilon)
            {
                var start = (int)Math.Floor(lowerIndex);
                var end = (int)Math.Ceiling(centerIndex) - 1;
                for (var w = start; w <= end; w++)
                {
                    var binLeft = w == start ? lowerIndex : w;
                    var binRight = w == end ? centerIndex : w + 1;
                    var binCenter = (binLeft + binRight) / 2;
                    var binArea = (binCenter - lowerIndex) / leftWidth * (binRight - binLeft);
                    num += binArea * source[w];
                    den += binArea;
                    count++;
                }
            }

            var rightWidth = upperIndex - centerIndex;
            if (rightWidth >= epsilon)
            {
                var start = (int)Math.Floor(centerIndex);
                var end = (int)Math.Ceiling(upperIndex) - 1;
                for (var w = start; w <= end; w++)
                {
                    var binLeft = w == start ? centerIndex : w;
                    var binRight = w == end ? upperIndex : w + 1;
                    var binCenter = (binLeft + binRight) / 2;
                    var binArea = (1 - (binCenter - centerIndex) / rightWidth) * (binRight - binLeft);
                    num += binArea * source[w];
                    den += binArea;
                    count++;
                }
            }

            if (count > 0)
            {
                return num / den;
            }
            else
            {
                return source[(int)Math.Floor(centerIndex)];
            }
        }
    }
}
