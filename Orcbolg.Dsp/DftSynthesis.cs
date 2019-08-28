using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace Orcbolg.Dsp
{
    public static class DftSynthesis
    {
        public static void Mirror(Complex[] dft)
        {
            if (dft == null) throw new ArgumentNullException(nameof(dft));
            if (dft.Length == 0) throw new ArgumentException("The length of the DFT must be greater than zero.", nameof(dft));
            if (dft.Length % 2 != 0) throw new ArgumentException("The length of the DFT must be even.", nameof(dft));

            var half = dft.Length / 2;
            for (var w = 1; w < half; w++)
            {
                dft[dft.Length - w] = dft[w].Conjugate();
            }
        }
    }
}
