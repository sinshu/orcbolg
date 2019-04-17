using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MathNet.Numerics;

namespace Orcbolg.Recog
{
    public static class SpectralAnalysis
    {
        // The component at the Nyquist frequency is discarded due to some reasons.
        public static void GetNormalizedAmplitude(Complex[] source, double[] destination)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            var half = source.Length / 2;
            if (destination.Length != half) throw new ArgumentException("The length of the destination must be half the length of the source.");

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
            if (destination.Length != half) throw new ArgumentException("The length of the destination must be half the length of the source.");

            destination[0] = scale * source[0].Magnitude / source.Length;
            for (var w = 1; w < destination.Length; w++)
            {
                destination[w] = scale * source[w].Magnitude / half;
            }
        }

        public static double GetEnergyWithTriangularFilter(double[] source, int sampleRate, int frameLength, double lowerFrequency, double centerFrequency, double upperFrequency)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException("The sample rate must be greater than zero.");
            if (frameLength <= 0) throw new ArgumentOutOfRangeException("The frame length must be greater than zero.");

            var half = frameLength / 2;
            if (source.Length != half) throw new ArgumentException("The length of the source must be half the length of the frame length.");

            var scale = (double)frameLength / sampleRate;
            var lowerIndex = (int)Math.Round(scale * lowerFrequency);
            var centerIndex = (int)Math.Round(scale * centerFrequency);
            var upperIndex = (int)Math.Round(scale * upperFrequency);

            if (!(0 <= lowerIndex && lowerIndex < half)) throw new ArgumentOutOfRangeException(nameof(lowerFrequency));
            if (!(0 <= centerIndex && centerIndex < half)) throw new ArgumentOutOfRangeException(nameof(centerFrequency));
            if (!(0 <= upperIndex && upperIndex <= half)) throw new ArgumentOutOfRangeException(nameof(upperFrequency));

            if (lowerIndex > centerIndex) throw new ArgumentOutOfRangeException("The lower frequency must be lower than or equal to the center frequency.");
            if (upperIndex < centerIndex) throw new ArgumentOutOfRangeException("The upper frequency must be higher than or equal to the center frequency.");

            var num = source[centerIndex];
            var den = 1.0;
            var leftWidth = centerIndex - lowerIndex;
            for (var w = lowerIndex + 1; w < centerIndex; w++)
            {
                var weight = (double)(w - lowerIndex) / leftWidth;
                num += weight * source[w];
                den += weight;
            }
            var rightWidth = upperIndex - centerIndex;
            for (var w = centerIndex + 1; w < upperIndex; w++)
            {
                var weight = 1 - (double)(w - centerIndex) / rightWidth;
                num += weight * source[w];
                den += weight;
            }
            return num / den;
        }
    }
}
