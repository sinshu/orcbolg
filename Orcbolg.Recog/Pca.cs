using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace Orcbolg.Recog
{
    public sealed class Pca
    {
        private Vector<double> mean;
        private Matrix<double> projection;

        private Pca()
        {
        }

        public static Pca FromVectors(IReadOnlyList<Vector<double>> xs)
        {
            var mean = Stats.Mean(xs);
            var covariance = Stats.Covariance(xs, mean);
            var svd = covariance.Svd();
            var projection = svd.VT;

            var pca = new Pca();
            pca.mean = mean;
            pca.projection = projection;
            return pca;
        }

        public static Pca FromMeanAndCovariance(Vector<double> mean, Matrix<double> covariance)
        {
            var svd = covariance.Svd();
            var projection = svd.VT;

            var pca = new Pca();
            pca.mean = mean;
            pca.projection = projection;
            return pca;
        }

        public static Pca FromMeanAndProjection(Vector<double> mean, Matrix<double> projection)
        {
            var pca = new Pca();
            pca.mean = mean;
            pca.projection = projection;
            return pca;
        }

        public IEnumerable<string> Serialize()
        {
            yield return "Pca";
            yield return "Mean";
            yield return string.Join(",", mean);
            yield return "Projection";
            foreach (var row in projection.EnumerateRows())
            {
                yield return string.Join(",", row);
            }
        }

        public static Pca Deserialize(IEnumerable<string> source)
        {
            string header;
            using (var enumerator = source.GetEnumerator())
            {
                enumerator.MoveNext();
                header = enumerator.Current;
                if (header != "Pca")
                {
                    throw new Exception("Invalid header (expected: Pca, actual: " + header + ").");
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
                if (header != "Projection")
                {
                    throw new Exception("Invalid header (expected: Projection, actual: " + header + ").");
                }

                var rows = new List<double[]>();
                for (var i = 0; i < mean.Count; i++)
                {
                    enumerator.MoveNext();
                    var line = enumerator.Current;
                    rows.Add(line.Split(',').Select(x => double.Parse(x)).ToArray());
                }
                var projection = DenseMatrix.OfRowArrays(rows);

                return FromMeanAndProjection(mean, projection);
            }
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

        public Matrix<double> Projection
        {
            get
            {
                return projection;
            }
        }
    }
}
