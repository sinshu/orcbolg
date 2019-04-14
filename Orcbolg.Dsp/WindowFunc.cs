using System;
using System.Collections.Generic;
using System.Linq;

namespace Orcbolg.Dsp
{
    public static class WindowFunc
    {
        public static double[] CreateHann(int length)
        {
            var window = new double[length];
            for (var t = 0; t < length; t++)
            {
                var x = 2 * Math.PI * t / length;
                window[t] = (1 - Math.Cos(x)) / 2;
            }
            return window;
        }

        public static double[] CreateSqrtHann(int length)
        {
            var window = new double[length];
            for (var t = 0; t < length; t++)
            {
                var x = 2 * Math.PI * t / length;
                window[t] = Math.Sqrt((1 - Math.Cos(x)) / 2);
            }
            return window;
        }
    }
}
