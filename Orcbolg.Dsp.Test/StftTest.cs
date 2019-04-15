using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Orcbolg.Dsp.Test
{
    [TestClass]
    public class StftTest
    {
        [TestMethod]
        public void Mirror()
        {
            var test = new Complex[8];
            test[0] = new Complex(1, 2);
            test[1] = new Complex(3, 4);
            test[2] = new Complex(5, 6);
            test[3] = new Complex(7, 8);
            test[4] = new Complex(9, 10);
            Stft.Mirror(test);
            Assert.IsTrue((test[0] - new Complex(1, 2)).Magnitude < 0.001);
            Assert.IsTrue((test[1] - new Complex(3, 4)).Magnitude < 0.001);
            Assert.IsTrue((test[2] - new Complex(5, 6)).Magnitude < 0.001);
            Assert.IsTrue((test[3] - new Complex(7, 8)).Magnitude < 0.001);
            Assert.IsTrue((test[4] - new Complex(9, 10)).Magnitude < 0.001);
            Assert.IsTrue((test[5] - new Complex(7, -8)).Magnitude < 0.001);
            Assert.IsTrue((test[6] - new Complex(5, -6)).Magnitude < 0.001);
            Assert.IsTrue((test[7] - new Complex(3, -4)).Magnitude < 0.001);
        }
    }
}
