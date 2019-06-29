using System;
using System.Collections.Generic;
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

        public IReadOnlyList<double> Weights
        {
            get
            {
                return weights;
            }
        }

        public IReadOnlyList<double> LogWeights
        {
            get
            {
                return logWeights;
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
