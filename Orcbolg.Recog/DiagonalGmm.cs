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
    public sealed class DiagonalGmm
    {
        private double[] weights;
        private double[] logWeights;
        private DiagonalGaussian[] gaussians;

        private DiagonalGmm()
        {
        }

        public static DiagonalGmm FromGaussians(IReadOnlyList<double> weights, IReadOnlyList<DiagonalGaussian> gaussians)
        {
            var gmm = new DiagonalGmm();
            gmm.weights = weights.ToArray();
            gmm.logWeights = weights.Select(w => Math.Log(w)).ToArray();
            gmm.gaussians = gaussians.ToArray();
            return gmm;
        }

        public double Pdf(Vector<double> x)
        {
            return Math.Exp(LogPdf(x));
        }

        public double LogPdf(Vector<double> x)
        {
            var logSum = double.NegativeInfinity;
            for (var i = 0; i < gaussians.Length; i++)
            {
                logSum = Calc.LogSum(logSum, logWeights[i] + gaussians[i].LogPdf(x));
            }
            return logSum;
        }

        public void Serialize(TextWriter writer)
        {
            writer.WriteLine("Count");
            writer.WriteLine(gaussians.Length);
            for (var i = 0; i < gaussians.Length; i++)
            {
                writer.WriteLine("Weight");
                writer.WriteLine(weights[i]);
                writer.WriteLine("DiagonalGaussian");
                gaussians[i].Serialize(writer);
            }
        }

        public static DiagonalGmm Deserialize(TextReader reader)
        {
            {
                var header = reader.ReadLine();
                if (header != "Count")
                {
                    throw new Exception("Invalid header (expected: Count, actual: " + header + ").");
                }
            }

            var count = int.Parse(reader.ReadLine());

            var weights = new double[count];
            var gaussians = new DiagonalGaussian[count];

            for (var i = 0; i < count; i++)
            {
                {
                    var header = reader.ReadLine();
                    if (header != "Weight")
                    {
                        throw new Exception("Invalid header (expected: Weight, actual: " + header + ").");
                    }
                }

                var weight = double.Parse(reader.ReadLine());

                {
                    var header = reader.ReadLine();
                    if (header != "DiagonalGaussian")
                    {
                        throw new Exception("Invalid header (expected: DiagonalGaussian, actual: " + header + ").");
                    }
                }

                var gaussian = DiagonalGaussian.Deserialize(reader);

                weights[i] = weight;
                gaussians[i] = gaussian;
            }

            return FromGaussians(weights, gaussians);
        }

        public int Count
        {
            get
            {
                return gaussians.Length;
            }
        }

        public IReadOnlyList<double> Weights
        {
            get
            {
                return weights;
            }
        }

        public IReadOnlyList<DiagonalGaussian> Gaussians
        {
            get
            {
                return gaussians;
            }
        }
    }
}
