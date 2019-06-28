using System;
using System.Collections.Generic;
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

        public Gaussian(IReadOnlyList<Vector<double>> xs) : this(xs, 0)
        {
        }

        public Gaussian(IReadOnlyList<Vector<double>> xs, double regularization)
        {
            mean = Stats.Mean(xs);
            covariance = Stats.Covariance(xs, mean) + regularization * DenseMatrix.CreateIdentity(mean.Count);
            cholesky = covariance.Cholesky();
            logNormalizationTerm = -(Math.Log(2 * Math.PI) * mean.Count + cholesky.DeterminantLn) / 2;
        }

        public Gaussian(Vector<double> mean, Matrix<double> covariance)
        {
            this.mean = mean;
            this.covariance = covariance;
            cholesky = covariance.Cholesky();
            logNormalizationTerm = -(Math.Log(2 * Math.PI) * mean.Count + cholesky.DeterminantLn) / 2;
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

        public IEnumerable<string> Serialize()
        {
            yield return "Gaussian";
            yield return "Mean";
            yield return string.Join(",", mean);
            yield return "Covariance";
            foreach (var row in covariance.EnumerateRows())
            {
                yield return string.Join(",", row);
            }
        }

        public static Gaussian Deserialize(IEnumerable<string> source)
        {
            string header;
            using (var enumerator = source.GetEnumerator())
            {
                enumerator.MoveNext();
                header = enumerator.Current;
                if (header != "Gaussian")
                {
                    throw new Exception("Invalid header (expected: Gaussian, actual: " + header + ").");
                }

                enumerator.MoveNext();
                header = enumerator.Current;
                if (header != "Mean")
                {
                    throw new Exception("Invalid header (expected: Mean, actual: " + header + ").");
                }

                enumerator.MoveNext();
                Vector<double> mean;
                {
                    var line = enumerator.Current;
                    mean = DenseVector.OfEnumerable(line.Split(',').Select(x => double.Parse(x)));
                }

                enumerator.MoveNext();
                header = enumerator.Current;
                if (header != "Covariance")
                {
                    throw new Exception("Invalid header (expected: Covariance, actual: " + header + ").");
                }

                var rows = new List<double[]>();
                for (var i = 0; i < mean.Count; i++)
                {
                    enumerator.MoveNext();
                    var line = enumerator.Current;
                    rows.Add(line.Split(',').Select(x => double.Parse(x)).ToArray());
                }
                var covariance = DenseMatrix.OfRowArrays(rows);

                return new Gaussian(mean, covariance);
            }
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
