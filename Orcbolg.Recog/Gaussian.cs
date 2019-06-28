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
