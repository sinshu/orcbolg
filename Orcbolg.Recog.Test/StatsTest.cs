using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Accord.Statistics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orcbolg.Recog;

namespace Orcbolg.Recog.Test
{
    [TestClass]
    public class StatsTest
    {
        [TestMethod]
        public void Mean()
        {
            var random = new Random(2357);
            for (var dimension = 1; dimension <= 10; dimension++)
            {
                var count = random.Next(2, 100);
                var a = DenseVector.OfEnumerable(Enumerable.Range(0, dimension).Select(d => 100 * random.NextDouble() - 50));
                var b = DenseVector.OfEnumerable(Enumerable.Range(0, dimension).Select(d => 100 * random.NextDouble() - 50));
                var xs = new List<Vector<double>>();
                for (var i = 0; i < count; i++)
                {
                    var x = DenseVector.OfEnumerable(Enumerable.Range(0, dimension).Select(d => a[d] + b[d] * random.NextDouble()));
                    xs.Add(x);
                }
                var actual = xs.Mean();
                var expected = Measures.Mean(xs.Select(x => x.ToArray()).ToArray(), 0);
                var error = actual - DenseVector.OfArray(expected);
                Assert.IsTrue(error.L2Norm() < 1.0E-9);
            }
        }

        [TestMethod]
        public void WeightedMean()
        {
            var random = new Random(2357);
            for (var dimension = 1; dimension <= 10; dimension++)
            {
                var count = random.Next(2, 100);
                var a = DenseVector.OfEnumerable(Enumerable.Range(0, dimension).Select(d => 100 * random.NextDouble() - 50));
                var b = DenseVector.OfEnumerable(Enumerable.Range(0, dimension).Select(d => 100 * random.NextDouble() - 50));
                var xs = new List<Vector<double>>();
                var weights = new List<double>();
                for (var i = 0; i < count; i++)
                {
                    var x = DenseVector.OfEnumerable(Enumerable.Range(0, dimension).Select(d => a[d] + b[d] * random.NextDouble()));
                    xs.Add(x);
                    weights.Add(random.NextDouble());
                }
                var actual = xs.WeightedMean(weights);
                var expected = Measures.WeightedMean(xs.Select(x => x.ToArray()).ToArray(), weights.ToArray(), 0);
                var error = actual - DenseVector.OfArray(expected);
                Assert.IsTrue(error.L2Norm() < 1.0E-9);
            }
        }

        [TestMethod]
        public void Variance()
        {
            var random = new Random(2357);
            for (var dimension = 1; dimension <= 10; dimension++)
            {
                var count = random.Next(2, 100);
                var a = DenseVector.OfEnumerable(Enumerable.Range(0, dimension).Select(d => 100 * random.NextDouble() - 50));
                var b = DenseVector.OfEnumerable(Enumerable.Range(0, dimension).Select(d => 100 * random.NextDouble() - 50));
                var xs = new List<Vector<double>>();
                for (var i = 0; i < count; i++)
                {
                    var x = DenseVector.OfEnumerable(Enumerable.Range(0, dimension).Select(d => a[d] + b[d] * random.NextDouble()));
                    xs.Add(x);
                }
                var actual = xs.Variance();
                var expected = Measures.Variance(xs.Select(x => x.ToArray()).ToArray());
                var error = actual - DenseVector.OfArray(expected);
                Assert.IsTrue(error.L2Norm() < 1.0E-9);
            }
        }

        [TestMethod]
        public void WeightedVariance()
        {
            var random = new Random(2357);
            for (var dimension = 1; dimension <= 10; dimension++)
            {
                var count = random.Next(2, 100);
                var a = DenseVector.OfEnumerable(Enumerable.Range(0, dimension).Select(d => 100 * random.NextDouble() - 50));
                var b = DenseVector.OfEnumerable(Enumerable.Range(0, dimension).Select(d => 100 * random.NextDouble() - 50));
                var xs = new List<Vector<double>>();
                var weights = new List<double>();
                for (var i = 0; i < count; i++)
                {
                    var x = DenseVector.OfEnumerable(Enumerable.Range(0, dimension).Select(d => a[d] + b[d] * random.NextDouble()));
                    xs.Add(x);
                    weights.Add(random.NextDouble());
                }
                var actual = xs.WeightedVariance(weights);
                var expected = Measures.WeightedVariance(xs.Select(x => x.ToArray()).ToArray(), weights.ToArray());
                var error = actual - DenseVector.OfArray(expected);
                Assert.IsTrue(error.L2Norm() < 1.0E-9);
            }
        }

        [TestMethod]
        public void Covariance()
        {
            var random = new Random(2357);
            for (var dimension = 1; dimension <= 10; dimension++)
            {
                var count = random.Next(2, 100);
                var a = DenseVector.OfEnumerable(Enumerable.Range(0, dimension).Select(d => 100 * random.NextDouble() - 50));
                var b = DenseVector.OfEnumerable(Enumerable.Range(0, dimension).Select(d => 100 * random.NextDouble() - 50));
                var xs = new List<Vector<double>>();
                for (var i = 0; i < count; i++)
                {
                    var x = DenseVector.OfEnumerable(Enumerable.Range(0, dimension).Select(d => a[d] + b[d] * random.NextDouble()));
                    xs.Add(x);
                }
                var actual = xs.Covariance();
                var expected = Measures.Covariance(xs.Select(x => x.ToArray()).ToArray());
                var error = actual - DenseMatrix.OfRowArrays(expected);
                Assert.IsTrue(error.L2Norm() < 1.0E-9);
            }
        }

        [TestMethod]
        public void WeightedCovariance()
        {
            var random = new Random(2357);
            for (var dimension = 1; dimension <= 10; dimension++)
            {
                var count = random.Next(2, 100);
                var a = DenseVector.OfEnumerable(Enumerable.Range(0, dimension).Select(d => 100 * random.NextDouble() - 50));
                var b = DenseVector.OfEnumerable(Enumerable.Range(0, dimension).Select(d => 100 * random.NextDouble() - 50));
                var xs = new List<Vector<double>>();
                var weights = new List<double>();
                for (var i = 0; i < count; i++)
                {
                    var x = DenseVector.OfEnumerable(Enumerable.Range(0, dimension).Select(d => a[d] + b[d] * random.NextDouble()));
                    xs.Add(x);
                    weights.Add(random.NextDouble());
                }
                var actual = xs.WeightedCovariance(weights);
                var expected = Measures.WeightedCovariance(xs.Select(x => x.ToArray()).ToArray(), weights.ToArray());
                var error = actual - DenseMatrix.OfArray(expected);
                Assert.IsTrue(error.L2Norm() < 1.0E-9);
            }
        }
    }
}
