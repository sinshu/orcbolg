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

        public void Serialize(TextWriter writer)
        {
            writer.WriteLine("Mean");
            writer.WriteLine(string.Join(",", mean));
            writer.WriteLine("Projection");
            foreach (var row in projection.EnumerateRows())
            {
                writer.WriteLine(string.Join(",", row));
            }
        }

        public static Pca Deserialize(TextReader reader)
        {
            {
                var header = reader.ReadLine();
                if (header != "Mean")
                {
                    throw new Exception("Invalid header (expected: Mean, actual: " + header + ").");
                }
            }

            var mean = DenseVector.OfEnumerable(reader.ReadLine().Split(',').Select(x => double.Parse(x))); ;

            {
                var header = reader.ReadLine();
                if (header != "Projection")
                {
                    throw new Exception("Invalid header (expected: Projection, actual: " + header + ").");
                }
            }

            var rows = new List<double[]>();
            for (var i = 0; i < mean.Count; i++)
            {
                rows.Add(reader.ReadLine().Split(',').Select(x => double.Parse(x)).ToArray());
            }
            var projection = DenseMatrix.OfRowArrays(rows);

            return FromMeanAndProjection(mean, projection);
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
