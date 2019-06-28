using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Orcbolg.Dsp
{
    public static class DftAnalysis
    {
        public static void GetNormalizedAmplitude(Complex[] dft, double[] destination)
        {
            if (dft == null) throw new ArgumentNullException(nameof(dft));
            if (dft.Length == 0) throw new ArgumentException("The length of the DFT must be greater than zero.", nameof(dft));
            if (dft.Length % 2 != 0) throw new ArgumentException("The length of the DFT must be even.", nameof(dft));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            var half = dft.Length / 2;
            if (destination.Length != half + 1) throw new ArgumentException("The length of the destination must be 2 * n + 1, where the n is the length of the DFT.");

            destination[0] = dft[0].Magnitude / dft.Length;
            for (var w = 1; w < destination.Length; w++)
            {
                destination[w] = dft[w].Magnitude / half;
            }
        }

        public static void GetNormalizedAmplitude(Complex[] dft, double[] destination, double scale)
        {
            if (dft == null) throw new ArgumentNullException(nameof(dft));
            if (dft.Length == 0) throw new ArgumentException("The length of the DFT must be greater than zero.", nameof(dft));
            if (dft.Length % 2 != 0) throw new ArgumentException("The length of the DFT must be even.", nameof(dft));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            var half = dft.Length / 2;
            if (destination.Length != half + 1) throw new ArgumentException("The length of the destination must be 2 * n + 1, where the n is the length of the DFT.");

            destination[0] = scale * dft[0].Magnitude / dft.Length;
            for (var w = 1; w < destination.Length; w++)
            {
                destination[w] = scale * dft[w].Magnitude / half;
            }
        }

        public static double GetEnergyWithTriangularFilter(double[] amplitude, int sampleRate, int dftLength, double lowerFrequency, double centerFrequency, double upperFrequency)
        {
            if (amplitude == null) throw new ArgumentNullException(nameof(amplitude));
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException("The sample rate must be greater than zero.", nameof(sampleRate));
            if (dftLength <= 0) throw new ArgumentOutOfRangeException("The DFT length must be greater than zero.", nameof (dftLength));
            if (amplitude.Length != dftLength / 2 + 1) throw new ArgumentException("The number of the amplitude values must be 2 * n + 1, where the n is the length of the DFT.");

            var scale = (double)dftLength / sampleRate;
            var lowerIndex = (int)Math.Round(scale * lowerFrequency);
            var centerIndex = (int)Math.Round(scale * centerFrequency);
            var upperIndex = (int)Math.Round(scale * upperFrequency);

            if (!(0 <= lowerIndex && lowerIndex < amplitude.Length)) throw new ArgumentOutOfRangeException(nameof(lowerFrequency));
            if (!(0 <= centerIndex && centerIndex < amplitude.Length)) throw new ArgumentOutOfRangeException(nameof(centerFrequency));
            if (!(0 <= upperIndex && upperIndex <= amplitude.Length)) throw new ArgumentOutOfRangeException(nameof(upperFrequency));

            if (lowerIndex > centerIndex) throw new ArgumentOutOfRangeException("The lower frequency must be lower than or equal to the center frequency.");
            if (upperIndex < centerIndex) throw new ArgumentOutOfRangeException("The upper frequency must be higher than or equal to the center frequency.");

            var num = amplitude[centerIndex];
            var den = 1.0;
            var leftWidth = centerIndex - lowerIndex;
            for (var w = lowerIndex + 1; w < centerIndex; w++)
            {
                var weight = (double)(w - lowerIndex) / leftWidth;
                num += weight * amplitude[w];
                den += weight;
            }
            var rightWidth = upperIndex - centerIndex;
            for (var w = centerIndex + 1; w < upperIndex; w++)
            {
                var weight = 1 - (double)(w - centerIndex) / rightWidth;
                num += weight * amplitude[w];
                den += weight;
            }
            return num / den;
        }
    }
}
