using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace Orcbolg.Recog
{
    public static class Stats
    {
        public static Vector<double> Mean(this IReadOnlyList<Vector<double>> xs)
        {
            Vector<double> sum = new DenseVector(xs[0].Count);
            var count = 0;
            foreach (var x in xs)
            {
                sum += x;
                count++;
            }
            return sum / count;
        }

        public static Vector<double> Variance(this IReadOnlyList<Vector<double>> xs)
        {
            var mean = Mean(xs);
            return Variance(xs, mean);
        }

        public static Vector<double> Variance(IReadOnlyList<Vector<double>> xs, Vector<double> mean)
        {
            Vector<double> sum = new DenseVector(xs[0].Count);
            var count = 0;
            foreach (var x in xs)
            {
                var d = x - mean;
                sum += d.PointwiseMultiply(d);
                count++;
            }
            return sum / (count - 1);
        }

        public static Matrix<double> Covariance(this IReadOnlyList<Vector<double>> xs)
        {
            var mean = Mean(xs);
            return Covariance(xs, mean);
        }

        public static Matrix<double> Covariance(IReadOnlyList<Vector<double>> xs, Vector<double> mean)
        {
            Matrix<double> sum = new DenseMatrix(xs[0].Count);
            var count = 0;
            foreach (var x in xs)
            {
                var d = x - mean;
                sum += d.OuterProduct(d);
                count++;
            }
            return sum / (count - 1);
        }
    }
}
