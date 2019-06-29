using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace Orcbolg.Recog
{
    public static class Calc
    {
        public static double Log1p(double x)
        {
            if (Math.Abs(x) > 1.0E-4)
            {
                return Math.Log(1.0 + x);
            }
            else
            {
                return (-0.5 * x + 1.0) * x;
            }
        }

        public static double LogSum(double x, double y)
        {
            if (double.IsNegativeInfinity(x))
            {
                return y;
            }

            if (double.IsNegativeInfinity(y))
            {
                return x;
            }

            if (x > y)
            {
                return x + Log1p(Math.Exp(y - x));
            }
            else
            {
                return y + Log1p(Math.Exp(x - y));
            }
        }
    }
}
