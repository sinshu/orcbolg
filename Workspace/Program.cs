using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Orcbolg.Dsp;
using Orcbolg.Recog;

static class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine(Calc.Log1p(1.0E-2));
    }
}
