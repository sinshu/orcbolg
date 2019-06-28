using System;
using System.Collections.Generic;
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
        private static IReadOnlyList<Vector<double>> CreateTestData()
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

        [TestMethod]
        public void Projection()
        {
            var xs = CreateTestData();
            var actualPca = new Pca(xs);
            var expectedPca = new PrincipalComponentAnalysis();
            expectedPca.Learn(xs.Select(x => x.ToArray()).ToArray());
            var actual = actualPca.Projection;
            var expected = DenseMatrix.OfRowArrays(expectedPca.ComponentVectors);
            var error = actual - expected;
            Assert.IsTrue(error.L2Norm() < 1.0E-9);
        }

        [TestMethod]
        public void Transform()
        {
            var xs = CreateTestData();
            var actualPca = new Pca(xs);
            var expectedPca = new PrincipalComponentAnalysis();
            expectedPca.Learn(xs.Select(x => x.ToArray()).ToArray());

            var random = new Random(4567);
            for (var i = 0; i < 10; i++)
            {
                var x = DenseVector.OfEnumerable(Enumerable.Range(0, xs[0].Count).Select(d => 100 * random.NextDouble() - 50));
                var actual = actualPca.Transform(x);
                var expected = DenseVector.OfArray(expectedPca.Transform(x.ToArray()));
                var error = actual - expected;
                Assert.IsTrue(error.L2Norm() < 1.0E-9);
            }
        }

        [TestMethod]
        public void Revert()
        {
            var xs = CreateTestData();
            var actualPca = new Pca(xs);
            var expectedPca = new PrincipalComponentAnalysis();
            expectedPca.Learn(xs.Select(x => x.ToArray()).ToArray());

            var random = new Random(4567);
            for (var i = 0; i < 10; i++)
            {
                var x = DenseVector.OfEnumerable(Enumerable.Range(0, xs[0].Count).Select(d => 100 * random.NextDouble() - 50));
                var actual = actualPca.Revert(x);
                var expected = DenseVector.OfArray(expectedPca.Revert(new[] { x.ToArray() })[0]);
                var error = actual - expected;
                Assert.IsTrue(error.L2Norm() < 1.0E-9);
            }
        }
    }
}
