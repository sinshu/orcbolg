using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Factorization;

namespace Orcbolg.Recog
{
    public sealed class DiagonalGaussian
    {
        private Vector<double> mean;
        private Vector<double> variance;
        private double logDeterminant;
        private double logNormalizationTerm;

        private DiagonalGaussian()
        {
        }

        public static DiagonalGaussian FromVectors(IReadOnlyList<Vector<double>> xs)
        {
            return FromVectors(xs, 0);
        }

        public static DiagonalGaussian FromVectors(IReadOnlyList<Vector<double>> xs, double regularization)
        {
            var mean = Stats.Mean(xs);
            var variance = Stats.Variance(xs, mean) + DenseVector.Create(mean.Count, regularization);
            var logDeterminant = variance.Select(a => Math.Log(a)).Sum();
            var logNormalizationTerm = -(Math.Log(2 * Math.PI) * mean.Count + logDeterminant) / 2;

            var gaussian = new DiagonalGaussian();
            gaussian.mean = mean;
            gaussian.variance = variance;
            gaussian.logDeterminant = logDeterminant;
            gaussian.logNormalizationTerm = logNormalizationTerm;
            return gaussian;
        }

        public static DiagonalGaussian FromMeanAndVariance(Vector<double> mean, Vector<double> variance)
        {
            var logDeterminant = variance.Select(a => Math.Log(a)).Sum();
            var logNormalizationTerm = -(Math.Log(2 * Math.PI) * mean.Count + logDeterminant) / 2;

            var gaussian = new DiagonalGaussian();
            gaussian.mean = mean;
            gaussian.variance = variance;
            gaussian.logDeterminant = logDeterminant;
            gaussian.logNormalizationTerm = logNormalizationTerm;
            return gaussian;
        }

        public double Pdf(Vector<double> x)
        {
            return Math.Exp(LogPdf(x));
        }

        public double LogPdf(Vector<double> x)
        {
            var d = x - mean;
            var m = d.PointwiseDivide(variance) * d;
            return logNormalizationTerm - m / 2;
        }

        public double Mahalanobis(Vector<double> x)
        {
            var d = x - mean;
            return Math.Sqrt(d.PointwiseDivide(variance) * d);
        }

        public double Bhattacharyya(DiagonalGaussian gaussian)
        {
            var meanVariance = (variance + gaussian.variance) / 2;
            var meanVarianceLogDeterminant = meanVariance.Select(a => Math.Log(a)).Sum();
            var d = gaussian.mean - mean;
            var m = d.PointwiseDivide(meanVariance) * d;
            return m / 8 + (meanVarianceLogDeterminant - (logDeterminant + gaussian.logDeterminant) / 2) / 2;
        }

        public void Serialize(TextWriter writer)
        {
            writer.WriteLine("Mean");
            writer.WriteLine(string.Join(",", mean));
            writer.WriteLine("Variance");
            writer.WriteLine(string.Join(",", variance));
        }

        public static DiagonalGaussian Deserialize(TextReader reader)
        {
            {
                var header = reader.ReadLine();
                if (header != "Mean")
                {
                    throw new Exception("Invalid header (expected: Mean, actual: " + header + ").");
                }
            }

            var mean = DenseVector.OfEnumerable(reader.ReadLine().Split(',').Select(x => double.Parse(x)));

            {
                var header = reader.ReadLine();
                if (header != "Variance")
                {
                    throw new Exception("Invalid header (expected: Variance, actual: " + header + ").");
                }
            }

            var variance = DenseVector.OfEnumerable(reader.ReadLine().Split(',').Select(x => double.Parse(x)));

            return FromMeanAndVariance(mean, variance);
        }

        public Vector<double> Mean
        {
            get
            {
                return mean;
            }
        }

        public Vector<double> Variance
        {
            get
            {
                return variance;
            }
        }
    }
}
