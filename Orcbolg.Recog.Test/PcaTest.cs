using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Accord.Statistics.Analysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orcbolg.Recog;

namespace Orcbolg.Recog.Test
{
    [TestClass]
    public class PcaTest
    {
        private static readonly double maxError = 1.0E-8;

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
        public void Projection1()
        {
            foreach (var xs in EnumTestData())
            {
                var actualPca = Pca.FromVectors(xs);
                var expectedPca = new PrincipalComponentAnalysis();
                expectedPca.Learn(xs.Select(x => x.ToArray()).ToArray());
                var actual = actualPca.Projection;
                var expected = DenseMatrix.OfRowArrays(expectedPca.ComponentVectors);
                var error1 = actual - expected;
                var error2 = actual + expected;
                var error = error1.EnumerateRows().Zip(error2.EnumerateRows(), (r1, r2) => Math.Min(r1.L2Norm(), r2.L2Norm()));
                Assert.IsTrue(error.All(e => e < maxError));
            }
        }

        [TestMethod]
        public void Projection2()
        {
            foreach (var xs in EnumTestData())
            {
                var mean = xs.Mean();
                var covariance = xs.Covariance();
                var actualPca = Pca.FromMeanAndCovariance(mean, covariance);
                var expectedPca = new PrincipalComponentAnalysis();
                expectedPca.Learn(xs.Select(x => x.ToArray()).ToArray());
                var actual = actualPca.Projection;
                var expected = DenseMatrix.OfRowArrays(expectedPca.ComponentVectors);
                var error1 = actual - expected;
                var error2 = actual + expected;
                var error = error1.EnumerateRows().Zip(error2.EnumerateRows(), (r1, r2) => Math.Min(r1.L2Norm(), r2.L2Norm()));
                Assert.IsTrue(error.All(e => e < maxError));
            }
        }

        [TestMethod]
        public void Transform()
        {
            foreach (var xs in EnumTestData())
            {
                var expectedPca = new PrincipalComponentAnalysis();
                expectedPca.Learn(xs.Select(x => x.ToArray()).ToArray());
                var mean = DenseVector.OfArray(expectedPca.Means);
                var projection = DenseMatrix.OfRowArrays(expectedPca.ComponentVectors);
                var actualPca = Pca.FromMeanAndProjection(mean, projection);

                var random = new Random(4567);
                for (var i = 0; i < 10; i++)
                {
                    var x = DenseVector.OfEnumerable(Enumerable.Range(0, xs[0].Count).Select(d => 100 * random.NextDouble() - 50));
                    var actual = actualPca.Transform(x);
                    var expected = DenseVector.OfArray(expectedPca.Transform(x.ToArray()));
                    var error = actual - expected;
                    Assert.IsTrue(error.L2Norm() < maxError);
                }
            }
        }

        [TestMethod]
        public void Revert()
        {
            foreach (var xs in EnumTestData())
            {
                var expectedPca = new PrincipalComponentAnalysis();
                expectedPca.Learn(xs.Select(x => x.ToArray()).ToArray());
                var mean = DenseVector.OfArray(expectedPca.Means);
                var projection = DenseMatrix.OfRowArrays(expectedPca.ComponentVectors);
                var actualPca = Pca.FromMeanAndProjection(mean, projection);

                var random = new Random(4567);
                for (var i = 0; i < 10; i++)
                {
                    var x = DenseVector.OfEnumerable(Enumerable.Range(0, xs[0].Count).Select(d => 100 * random.NextDouble() - 50));
                    var actual = actualPca.Revert(x);
                    var expected = DenseVector.OfArray(expectedPca.Revert(new[] { x.ToArray() })[0]);
                    var error = actual - expected;
                    Assert.IsTrue(error.L2Norm() < maxError);
                }
            }
        }

        [TestMethod]
        public void SerializeDeserialize()
        {
            foreach (var xs in EnumTestData())
            {
                var pca1 = Pca.FromVectors(xs);
                using (var writer = new StreamWriter("pca.csv"))
                {
                    pca1.Serialize(writer);
                }

                Pca pca2;
                using (var reader = new StreamReader("pca.csv"))
                {
                    pca2 = Pca.Deserialize(reader);
                }

                var meanError = pca1.Mean - pca2.Mean;
                Assert.IsTrue(meanError.L2Norm() < maxError);

                var projError = pca1.Projection - pca2.Projection;
                Assert.IsTrue(projError.L2Norm() < maxError);
            }
        }
    }
}
