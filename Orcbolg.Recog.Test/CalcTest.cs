using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Accord.Math;
using Accord.Statistics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orcbolg.Recog;

namespace Orcbolg.Recog.Test
{
    [TestClass]
    public class CalcTest
    {
        [TestMethod]
        public void Log1pTest()
        {
            for (var i = -10; i <= 10; i++)
            {
                var x = Math.Pow(10, i);
                Assert.AreEqual(Special.Log1p(x), Calc.Log1p(x), 1.0E-16);
            }
        }

        [TestMethod]
        public void LogSumTest()
        {
            var random = new Random(2357);
            for (var i = 0; i < 30; i++)
            {
                var a = 10 + 10 * random.NextDouble();
                var b = 10 + 10 * random.NextDouble();

                var logA = Math.Log(a);
                var logB = Math.Log(b);

                var c = a + b;
                var logC = Calc.LogSum(logA, logB);

                var actual = Math.Exp(logC);
                var expected = c;
                Assert.AreEqual(expected, actual, 1.0E-9);
            }
        }
    }
}
