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
    public sealed class Gaussian
    {
        private Vector<double> mean;
        private Matrix<double> covariance;
        private Cholesky<double> cholesky;
        private double logNormalizationTerm;

        private Gaussian()
        {
        }

        public static Gaussian FromVectors(IReadOnlyList<Vector<double>> xs)
        {
            return FromVectors(xs, 0);
        }

        public static Gaussian FromVectors(IReadOnlyList<Vector<double>> xs, double regularization)
        {
            var mean = Stats.Mean(xs);
            var covariance = Stats.Covariance(xs, mean) + DenseMatrix.CreateDiagonal(mean.Count, mean.Count, regularization);
            var cholesky = covariance.Cholesky();
            var logNormalizationTerm = -(Math.Log(2 * Math.PI) * mean.Count + cholesky.DeterminantLn) / 2;

            var gaussian = new Gaussian();
            gaussian.mean = mean;
            gaussian.covariance = covariance;
            gaussian.cholesky = cholesky;
            gaussian.logNormalizationTerm = logNormalizationTerm;
            return gaussian;
        }

        public static Gaussian FromMeanAndCovariance(Vector<double> mean, Matrix<double> covariance)
        {
            var cholesky = covariance.Cholesky();
            var logNormalizationTerm = -(Math.Log(2 * Math.PI) * mean.Count + cholesky.DeterminantLn) / 2;

            var gaussian = new Gaussian();
            gaussian.mean = mean;
            gaussian.covariance = covariance;
            gaussian.cholesky = cholesky;
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
            var m = d * cholesky.Solve(d);
            return logNormalizationTerm - m / 2;
        }

        public double Mahalanobis(Vector<double> x)
        {
            var d = x - mean;
            return Math.Sqrt(d * cholesky.Solve(d));
        }

        public double Bhattacharyya(Gaussian gaussian)
        {
            var meanCovarianceCholesky = ((covariance + gaussian.covariance) / 2).Cholesky();
            var d = gaussian.mean - mean;
            var m = d * meanCovarianceCholesky.Solve(d);
            return m / 8 + (meanCovarianceCholesky.DeterminantLn - (cholesky.DeterminantLn + gaussian.cholesky.DeterminantLn) / 2) / 2;
        }

        public void Serialize(TextWriter writer)
        {
            writer.WriteLine("Mean");
            writer.WriteLine(string.Join(",", mean));
            writer.WriteLine("Covariance");
            foreach (var row in covariance.EnumerateRows())
            {
                writer.WriteLine(string.Join(",", row));
            }
        }

        public static Gaussian Deserialize(TextReader reader)
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
                if (header != "Covariance")
                {
                    throw new Exception("Invalid header (expected: Covariance, actual: " + header + ").");
                }
            }

            var rows = new List<double[]>();
            for (var i = 0; i < mean.Count; i++)
            {
                rows.Add(reader.ReadLine().Split(',').Select(x => double.Parse(x)).ToArray());
            }
            var covariance = DenseMatrix.OfRowArrays(rows);

            return FromMeanAndCovariance(mean, covariance);
        }

        public Vector<double> Mean
        {
            get
            {
                return mean;
            }
        }

        public Matrix<double> Covariance
        {
            get
            {
                return covariance;
            }
        }
    }
}
