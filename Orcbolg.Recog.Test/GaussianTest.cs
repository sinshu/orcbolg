using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Accord.Statistics.Analysis;
using Accord.Statistics.Distributions.Multivariate;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orcbolg.Recog;

namespace Orcbolg.Recog.Test
{
    [TestClass]
    public class GaussianTest
    {
        private static IReadOnlyList<Vector<double>> CreateTestData()
        {
            var random = new Random(2357);
            var xs = new List<Vector<double>>();
            for (var i = 0; i < 100; i++)
            {
                var a = i + random.NextDouble();
                var b = (a + random.NextDouble()) / 2;
                var c = (b + random.NextDouble()) / 3;
                var x = DenseVector.OfArray(new[] { a, b, c });
                xs.Add(x);
            }
            return xs;
        }

        [TestMethod]
        public void MeanCovariance()
        {
            var xs = CreateTestData();
            var actualGaussian = new Gaussian(xs);
            var expectedGaussian = new MultivariateNormalDistribution(xs[0].Count);
            expectedGaussian.Fit(xs.Select(x => x.ToArray()).ToArray());
            var actualMean = actualGaussian.Mean;
            var actualCov = actualGaussian.Covariance;
            var expectedMean = DenseVector.OfArray(expectedGaussian.Mean);
            var expectedCov = DenseMatrix.OfArray(expectedGaussian.Covariance);
            var meanError = actualMean - expectedMean;
            Assert.IsTrue(meanError.L2Norm() < 1.0E-9);
            var covError = actualCov - expectedCov;
            Assert.IsTrue(covError.L2Norm() < 1.0E-9);
        }

        [TestMethod]
        public void Pdf()
        {
            var xs = CreateTestData();
            var mean = xs.Mean();
            var actualGaussian = new Gaussian(xs);
            var expectedGaussian = new MultivariateNormalDistribution(xs[0].Count);
            expectedGaussian.Fit(xs.Select(x => x.ToArray()).ToArray());

            var random = new Random(4567);
            for (var i = 0; i < 10; i++)
            {
                var x = mean + DenseVector.OfEnumerable(Enumerable.Range(0, xs[0].Count).Select(d => 0.2 * random.NextDouble() - 0.1));
                var actual = actualGaussian.Pdf(x);
                var expected = expectedGaussian.ProbabilityDensityFunction(x.ToArray());
                var error = actual - expected;
                Assert.IsTrue(error < 1.0E-9);
            }
        }

        [TestMethod]
        public void LogPdf()
        {
            var xs = CreateTestData();
            var mean = xs.Mean();
            var actualGaussian = new Gaussian(xs);
            var expectedGaussian = new MultivariateNormalDistribution(xs[0].Count);
            expectedGaussian.Fit(xs.Select(x => x.ToArray()).ToArray());

            var random = new Random(4567);
            for (var i = 0; i < 10; i++)
            {
                var x = mean + DenseVector.OfEnumerable(Enumerable.Range(0, xs[0].Count).Select(d => 10 * random.NextDouble() - 5));
                var actual = actualGaussian.LogPdf(x);
                var expected = expectedGaussian.LogProbabilityDensityFunction(x.ToArray());
                var error = actual - expected;
                Assert.IsTrue(error < 1.0E-9);
            }
        }

        [TestMethod]
        public void Mahalanobis()
        {
            var xs = CreateTestData();
            var mean = xs.Mean();
            var actualGaussian = new Gaussian(xs);
            var expectedGaussian = new MultivariateNormalDistribution(xs[0].Count);
            expectedGaussian.Fit(xs.Select(x => x.ToArray()).ToArray());

            var random = new Random(4567);
            for (var i = 0; i < 10; i++)
            {
                var x = mean + DenseVector.OfEnumerable(Enumerable.Range(0, xs[0].Count).Select(d => 10 * random.NextDouble() - 5));
                var actual = actualGaussian.Mahalanobis(x);
                var expected = Math.Sqrt(expectedGaussian.Mahalanobis(x.ToArray()));
                var error = actual - expected;
                Assert.IsTrue(error < 1.0E-9);
            }
        }
    }
}
