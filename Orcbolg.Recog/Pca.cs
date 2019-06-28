using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace Orcbolg.Recog
{
    public sealed class Pca
    {
        private Vector<double> mean;
        private Matrix<double> covariance;
        private Matrix<double> projection;

        public Pca(IReadOnlyList<Vector<double>> xs)
        {
            mean = Stats.Mean(xs);
            covariance = Stats.Covariance(xs, mean);
            var svd = covariance.Svd();
            projection = -svd.VT;
        }

        public Vector<double> Transform(Vector<double> x)
        {
            return projection * (x - mean);
        }

        public Vector<double> Revert(Vector<double> y)
        {
            return projection.TransposeThisAndMultiply(y) + mean;
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

        public Matrix<double> Projection
        {
            get
            {
                return projection;
            }
        }
    }
}
