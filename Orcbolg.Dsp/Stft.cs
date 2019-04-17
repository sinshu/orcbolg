using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace Orcbolg.Dsp
{
    public static class Stft
    {
        public static void Mirror(Complex[] stft)
        {
            if (stft == null) throw new ArgumentNullException(nameof(stft));
            if (stft.Length == 0) throw new ArgumentException("The length of STFT must be greater than zero.", nameof(stft));
            if (stft.Length % 2 != 0) throw new ArgumentException("The length of STFT must be even.", nameof(stft));

            var half = stft.Length / 2;
            for (var w = 1; w < half; w++)
            {
                stft[stft.Length - w] = stft[w].Conjugate();
            }
        }
    }
}
