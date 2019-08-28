using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Accord.Math.Metrics;
using Accord.Statistics.Distributions.Fitting;
using Accord.Statistics.Distributions.Multivariate;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orcbolg.Recog;
using System.IO;

namespace Orcbolg.Recog.Test
{
    [TestClass]
    public class GmmTest
    {
        private static readonly double maxError = 1.0E-9;

        [TestMethod]
        public void LogPdfTest()
        {
            var weight1 = 0.3;
            var mean1 = DenseVector.OfArray(new[] { 0.0, 0.1, 0.2 });
            var cov1 = DenseMatrix.OfArray(
                    new[,]
                    {
                        { 1.0, 0.1, 0.2 },
                        { 0.1, 2.0, 0.3 },
                        { 0.2, 0.3, 3.0 },
                    }
                );

            var weight2 = 0.5;
            var mean2 = DenseVector.OfArray(new[] { 0.5, 0.0, 0.1 });
            var cov2 = DenseMatrix.OfArray(
                    new[,]
                    {
                        { 2.0, 0.2, 0.1 },
                        { 0.2, 1.0, 0.1 },
                        { 0.1, 0.1, 0.5 },
                    }
                );

            var weight3 = 0.2;
            var mean3 = DenseVector.OfArray(new[] { 0.1, 0.3, 0.2 });
            var cov3 = DenseMatrix.OfArray(
                    new[,]
                    {
                        { 1.5, 0.3, 0.2 },
                        { 0.3, 0.8, 0.1 },
                        { 0.2, 0.1, 1.7 },
                    }
                );

            var weights = new[] { weight1, weight2, weight3 };

            var actualGaussian1 = Gaussian.FromMeanAndCovariance(mean1, cov1);
            var actualGaussian2 = Gaussian.FromMeanAndCovariance(mean2, cov2);
            var actualGaussian3 = Gaussian.FromMeanAndCovariance(mean3, cov3);
            var actualGaussians = new[] { actualGaussian1, actualGaussian2, actualGaussian3 };
            var actualGmm = Gmm.FromGaussians(weights, actualGaussians);

            var expectedGaussian1 = new MultivariateNormalDistribution(mean1.ToArray(), cov1.ToArray());
            var expectedGaussian2 = new MultivariateNormalDistribution(mean2.ToArray(), cov2.ToArray());
            var expectedGaussian3 = new MultivariateNormalDistribution(mean3.ToArray(), cov3.ToArray());
            var expectedGmm = new MultivariateMixture<MultivariateNormalDistribution>(weights, expectedGaussian1, expectedGaussian2, expectedGaussian3);

            var random = new Random(2357);
            for (var i = 0; i < 10; i++)
            {
                var x = mean1 + DenseVector.OfEnumerable(Enumerable.Range(0, mean1.Count).Select(d => 10 * random.NextDouble() - 5));
                var actual = actualGmm.LogPdf(x);
                var expected = expectedGmm.LogProbabilityDensityFunction(x.ToArray());
                Assert.AreEqual(expected, actual, maxError);
            }
            for (var i = 0; i < 10; i++)
            {
                var x = mean2 + DenseVector.OfEnumerable(Enumerable.Range(0, mean1.Count).Select(d => 20 * random.NextDouble() - 10));
                var actual = actualGmm.LogPdf(x);
                var expected = expectedGmm.LogProbabilityDensityFunction(x.ToArray());
                Assert.AreEqual(expected, actual, maxError);
            }
            for (var i = 0; i < 10; i++)
            {
                var x = mean3 + DenseVector.OfEnumerable(Enumerable.Range(0, mean1.Count).Select(d => 4 * random.NextDouble() - 2));
                var actual = actualGmm.LogPdf(x);
                var expected = expectedGmm.LogProbabilityDensityFunction(x.ToArray());
                Assert.AreEqual(expected, actual, maxError);
            }
        }

        [TestMethod]
        public void PdfTest()
        {
            var weight1 = 0.3;
            var mean1 = DenseVector.OfArray(new[] { 0.0, 0.1, 0.2 });
            var cov1 = DenseMatrix.OfArray(
                    new[,]
                    {
                        { 1.0, 0.1, 0.2 },
                        { 0.1, 2.0, 0.3 },
                        { 0.2, 0.3, 3.0 },
                    }
                );

            var weight2 = 0.5;
            var mean2 = DenseVector.OfArray(new[] { 0.5, 0.0, 0.1 });
            var cov2 = DenseMatrix.OfArray(
                    new[,]
                    {
                        { 2.0, 0.2, 0.1 },
                        { 0.2, 1.0, 0.1 },
                        { 0.1, 0.1, 0.5 },
                    }
                );

            var weight3 = 0.2;
            var mean3 = DenseVector.OfArray(new[] { 0.1, 0.3, 0.2 });
            var cov3 = DenseMatrix.OfArray(
                    new[,]
                    {
                        { 1.5, 0.3, 0.2 },
                        { 0.3, 0.8, 0.1 },
                        { 0.2, 0.1, 1.7 },
                    }
                );

            var weights = new[] { weight1, weight2, weight3 };

            var actualGaussian1 = Gaussian.FromMeanAndCovariance(mean1, cov1);
            var actualGaussian2 = Gaussian.FromMeanAndCovariance(mean2, cov2);
            var actualGaussian3 = Gaussian.FromMeanAndCovariance(mean3, cov3);
            var actualGaussians = new[] { actualGaussian1, actualGaussian2, actualGaussian3 };
            var actualGmm = Gmm.FromGaussians(weights, actualGaussians);

            var expectedGaussian1 = new MultivariateNormalDistribution(mean1.ToArray(), cov1.ToArray());
            var expectedGaussian2 = new MultivariateNormalDistribution(mean2.ToArray(), cov2.ToArray());
            var expectedGaussian3 = new MultivariateNormalDistribution(mean3.ToArray(), cov3.ToArray());
            var expectedGmm = new MultivariateMixture<MultivariateNormalDistribution>(weights, expectedGaussian1, expectedGaussian2, expectedGaussian3);

            var random = new Random(2357);
            for (var i = 0; i < 10; i++)
            {
                var x = mean1 + DenseVector.OfEnumerable(Enumerable.Range(0, mean1.Count).Select(d => 10 * random.NextDouble() - 5));
                var actual = actualGmm.Pdf(x);
                var expected = expectedGmm.ProbabilityDensityFunction(x.ToArray());
                Assert.AreEqual(expected, actual, maxError);
            }
            for (var i = 0; i < 10; i++)
            {
                var x = mean2 + DenseVector.OfEnumerable(Enumerable.Range(0, mean1.Count).Select(d => 20 * random.NextDouble() - 10));
                var actual = actualGmm.Pdf(x);
                var expected = expectedGmm.ProbabilityDensityFunction(x.ToArray());
                Assert.AreEqual(expected, actual, maxError);
            }
            for (var i = 0; i < 10; i++)
            {
                var x = mean3 + DenseVector.OfEnumerable(Enumerable.Range(0, mean1.Count).Select(d => 4 * random.NextDouble() - 2));
                var actual = actualGmm.Pdf(x);
                var expected = expectedGmm.ProbabilityDensityFunction(x.ToArray());
                Assert.AreEqual(expected, actual, maxError);
            }
        }

        [TestMethod]
        public void SerializeDeserialize()
        {
            var weight1 = 0.3;
            var mean1 = DenseVector.OfArray(new[] { 0.0, 0.1, 0.2 });
            var cov1 = DenseMatrix.OfArray(
                    new[,]
                    {
                        { 1.0, 0.1, 0.2 },
                        { 0.1, 2.0, 0.3 },
                        { 0.2, 0.3, 3.0 },
                    }
                );

            var weight2 = 0.5;
            var mean2 = DenseVector.OfArray(new[] { 0.5, 0.0, 0.1 });
            var cov2 = DenseMatrix.OfArray(
                    new[,]
                    {
                        { 2.0, 0.2, 0.1 },
                        { 0.2, 1.0, 0.1 },
                        { 0.1, 0.1, 0.5 },
                    }
                );

            var weight3 = 0.2;
            var mean3 = DenseVector.OfArray(new[] { 0.1, 0.3, 0.2 });
            var cov3 = DenseMatrix.OfArray(
                    new[,]
                    {
                        { 1.5, 0.3, 0.2 },
                        { 0.3, 0.8, 0.1 },
                        { 0.2, 0.1, 1.7 },
                    }
                );

            var weights = new[] { weight1, weight2, weight3 };

            var expectedGaussian1 = Gaussian.FromMeanAndCovariance(mean1, cov1);
            var expectedGaussian2 = Gaussian.FromMeanAndCovariance(mean2, cov2);
            var expectedGaussian3 = Gaussian.FromMeanAndCovariance(mean3, cov3);
            var expectedGaussians = new[] { expectedGaussian1, expectedGaussian2, expectedGaussian3 };
            var expectedGmm = Gmm.FromGaussians(weights, expectedGaussians);
            using (var writer = new StreamWriter("gmm.csv"))
            {
                expectedGmm.Serialize(writer);
            }

            Gmm actualGmm;
            using (var reader = new StreamReader("gmm.csv"))
            {
                actualGmm = Gmm.Deserialize(reader);
            }

            for (var i = 0; i < 3; i++)
            {
                var weightError = actualGmm.Weights[i] - expectedGmm.Weights[i];
                Assert.IsTrue(Math.Abs(weightError) < maxError);

                var meanError = actualGmm.Gaussians[i].Mean - expectedGmm.Gaussians[i].Mean;
                Assert.IsTrue(meanError.L2Norm() < maxError);

                var varError = actualGmm.Gaussians[i].Covariance - expectedGmm.Gaussians[i].Covariance;
                Assert.IsTrue(varError.L2Norm() < maxError);
            }
        }
    }
}
