using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Accord.Math.Distances;
using Accord.Statistics.Distributions.Fitting;
using Accord.Statistics.Distributions.Multivariate;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orcbolg.Recog;
using System.IO;

namespace Orcbolg.Recog.Test
{
    [TestClass]
    public class DiagonalGaussianTest
    {
        private static readonly double maxError = 1.0E-9;

        private static IReadOnlyList<Vector<double>> CreateTestData1()
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

        private static IReadOnlyList<Vector<double>> CreateTestData2()
        {
            var random = new Random(42);
            var xs = new List<Vector<double>>();
            for (var i = 0; i < 30; i++)
            {
                var a = i + random.NextDouble() * 3;
                var b = (a + random.NextDouble()) * 2;
                var c = (b + random.NextDouble()) * 1.5;
                var x = DenseVector.OfArray(new[] { a, b, c });
                xs.Add(x);
            }
            return xs;
        }

        private static IReadOnlyList<Vector<double>> CreateTestData3()
        {
            var random = new Random(1337);
            var xs = new List<Vector<double>>();
            for (var i = 0; i < 80; i++)
            {
                var a = i + random.NextDouble() + 50;
                var b = (a + random.NextDouble()) / 2 + 20;
                var c = (b + random.NextDouble()) / 3 + 30;
                var x = DenseVector.OfArray(new[] { a, b, c });
                xs.Add(x);
            }
            return xs;
        }

        private static IReadOnlyList<Vector<double>> CreateTestData4()
        {
            var random = new Random(2357);
            var xs = new List<Vector<double>>();
            for (var i = 0; i < 100; i++)
            {
                var a = i + random.NextDouble();
                var b = (a + random.NextDouble()) / 2;
                var c = (b + random.NextDouble()) / 3;
                var d = (c + random.NextDouble()) / 4;
                var x = DenseVector.OfArray(new[] { a, b, c, d });
                xs.Add(x);
            }
            return xs;
        }

        private static IEnumerable<IReadOnlyList<Vector<double>>> EnumTestData()
        {
            yield return CreateTestData1();
            yield return CreateTestData2();
            yield return CreateTestData3();
            yield return CreateTestData4();
        }

        [TestMethod]
        public void MeanCovariance()
        {
            foreach (var xs in EnumTestData())
            {
                var actualGaussian = DiagonalGaussian.FromVectors(xs);
                var expectedGaussian = new MultivariateNormalDistribution(xs[0].Count);
                var options = new NormalOptions();
                options.Diagonal = true;
                expectedGaussian.Fit(xs.Select(x => x.ToArray()).ToArray(), options);
                var actualMean = actualGaussian.Mean;
                var actualVar = actualGaussian.Variance;
                var expectedMean = DenseVector.OfArray(expectedGaussian.Mean);
                var expectedVar = DenseVector.OfArray(expectedGaussian.Variance);
                var meanError = actualMean - expectedMean;
                Assert.IsTrue(meanError.L2Norm() < maxError);
                var covError = actualVar - expectedVar;
                Assert.IsTrue(covError.L2Norm() < maxError);
            }
        }

        [TestMethod]
        public void Pdf()
        {
            foreach (var xs in EnumTestData())
            {
                var mean = xs.Mean();
                var actualGaussian = DiagonalGaussian.FromVectors(xs);
                var expectedGaussian = new MultivariateNormalDistribution(xs[0].Count);
                var options = new NormalOptions();
                options.Diagonal = true;
                expectedGaussian.Fit(xs.Select(x => x.ToArray()).ToArray(), options);

                var random = new Random(4567);
                for (var i = 0; i < 10; i++)
                {
                    var x = mean + DenseVector.OfEnumerable(Enumerable.Range(0, xs[0].Count).Select(d => 0.2 * random.NextDouble() - 0.1));
                    var actual = actualGaussian.Pdf(x);
                    var expected = expectedGaussian.ProbabilityDensityFunction(x.ToArray());
                    var error = actual - expected;
                    Assert.IsTrue(Math.Abs(error) < maxError);
                }
            }
        }

        [TestMethod]
        public void LogPdf()
        {
            foreach (var xs in EnumTestData())
            {
                var mean = xs.Mean();
                var actualGaussian = DiagonalGaussian.FromVectors(xs);
                var expectedGaussian = new MultivariateNormalDistribution(xs[0].Count);
                var options = new NormalOptions();
                options.Diagonal = true;
                expectedGaussian.Fit(xs.Select(x => x.ToArray()).ToArray(), options);

                var random = new Random(4567);
                for (var i = 0; i < 10; i++)
                {
                    var x = mean + DenseVector.OfEnumerable(Enumerable.Range(0, xs[0].Count).Select(d => 10 * random.NextDouble() - 5));
                    var actual = actualGaussian.LogPdf(x);
                    var expected = expectedGaussian.LogProbabilityDensityFunction(x.ToArray());
                    var error = actual - expected;
                    Assert.IsTrue(Math.Abs(error) < maxError);
                }
            }
        }

        [TestMethod]
        public void Mahalanobis1()
        {
            var xs = CreateTestData1();
            var mean = xs.Mean();
            var actualGaussian = DiagonalGaussian.FromVectors(xs);
            var expectedGaussian = new MultivariateNormalDistribution(xs[0].Count);
            var options = new NormalOptions();
            options.Diagonal = true;
            expectedGaussian.Fit(xs.Select(x => x.ToArray()).ToArray(), options);

            var random = new Random(4567);
            for (var i = 0; i < 10; i++)
            {
                var x = mean + DenseVector.OfEnumerable(Enumerable.Range(0, xs[0].Count).Select(d => 10 * random.NextDouble() - 5));
                var actual = actualGaussian.Mahalanobis(x);
                var expected = Math.Sqrt(expectedGaussian.Mahalanobis(x.ToArray()));
                var error = actual - expected;
                Assert.IsTrue(Math.Abs(error) < maxError);
            }
        }

        [TestMethod]
        public void Mahalanobis2()
        {
            var xs = CreateTestData1();
            var mean = xs.Mean();
            var variance = xs.Variance();
            var actualGaussian = DiagonalGaussian.FromMeanAndVariance(mean, variance);
            var expectedGaussian = new MultivariateNormalDistribution(xs[0].Count);
            var options = new NormalOptions();
            options.Diagonal = true;
            expectedGaussian.Fit(xs.Select(x => x.ToArray()).ToArray(), options);

            var random = new Random(4567);
            for (var i = 0; i < 10; i++)
            {
                var x = mean + DenseVector.OfEnumerable(Enumerable.Range(0, xs[0].Count).Select(d => 10 * random.NextDouble() - 5));
                var actual = actualGaussian.Mahalanobis(x);
                var expected = Math.Sqrt(expectedGaussian.Mahalanobis(x.ToArray()));
                Console.WriteLine(actual + ", " + expected);
                var error = actual - expected;
                Console.WriteLine(error);
                Assert.IsTrue(Math.Abs(error) < maxError);
            }
        }

        [TestMethod]
        public void Bhattacharyya1()
        {
            var xs1 = CreateTestData1();
            var xs2 = CreateTestData2();

            var actualGaussian1 = DiagonalGaussian.FromVectors(xs1);
            var actualGaussian2 = DiagonalGaussian.FromVectors(xs2);

            var options = new NormalOptions();
            options.Diagonal = true;
            var expectedGaussian1 = new MultivariateNormalDistribution(xs1[0].Count);
            expectedGaussian1.Fit(xs1.Select(x => x.ToArray()).ToArray(), options);
            var expectedGaussian2 = new MultivariateNormalDistribution(xs2[0].Count);
            expectedGaussian2.Fit(xs2.Select(x => x.ToArray()).ToArray(), options);
            var b = new Bhattacharyya();

            var actual = actualGaussian1.Bhattacharyya(actualGaussian2);
            var expected = b.Distance(expectedGaussian1, expectedGaussian2);
            var error = actual - expected;
            Assert.IsTrue(Math.Abs(error) < maxError);
        }

        [TestMethod]
        public void Bhattacharyya2()
        {
            var xs1 = CreateTestData2();
            var xs2 = CreateTestData3();

            var actualGaussian1 = DiagonalGaussian.FromVectors(xs1);
            var actualGaussian2 = DiagonalGaussian.FromVectors(xs2);

            var options = new NormalOptions();
            options.Diagonal = true;
            var expectedGaussian1 = new MultivariateNormalDistribution(xs1[0].Count);
            expectedGaussian1.Fit(xs1.Select(x => x.ToArray()).ToArray(), options);
            var expectedGaussian2 = new MultivariateNormalDistribution(xs2[0].Count);
            expectedGaussian2.Fit(xs2.Select(x => x.ToArray()).ToArray(), options);
            var b = new Bhattacharyya();

            var actual = actualGaussian1.Bhattacharyya(actualGaussian2);
            var expected = b.Distance(expectedGaussian1, expectedGaussian2);
            var error = actual - expected;
            Assert.IsTrue(Math.Abs(error) < maxError);
        }

        [TestMethod]
        public void SerializeDeserialize()
        {
            foreach (var xs in EnumTestData())
            {
                var gaussian1 = DiagonalGaussian.FromVectors(xs);
                using (var writer = new StreamWriter("diagonalgaussian.csv"))
                {
                    gaussian1.Serialize(writer);
                }

                DiagonalGaussian gaussian2;
                using (var reader = new StreamReader("diagonalgaussian.csv"))
                {
                    gaussian2 = DiagonalGaussian.Deserialize(reader);
                }

                var meanError = gaussian1.Mean - gaussian2.Mean;
                Assert.IsTrue(meanError.L2Norm() < maxError);

                var varError = gaussian1.Variance - gaussian2.Variance;
                Assert.IsTrue(varError.L2Norm() < maxError);
            }
        }

        [TestMethod]
        public void Regularization()
        {
            foreach (var xs in EnumTestData())
            {
                var gaussian = DiagonalGaussian.FromVectors(xs, 3);
                var actual = gaussian.Variance;
                var expected = xs.Variance() + DenseVector.OfEnumerable(Enumerable.Range(0, xs[0].Count).Select(i => 3.0));
                var error = actual - expected;
                Assert.IsTrue(error.L2Norm() < maxError);
            }
        }
    }
}
