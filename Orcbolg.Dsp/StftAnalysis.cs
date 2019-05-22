using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace Orcbolg.Dsp
{
    public sealed class StftAnalysis
    {
        private readonly int channelCount;
        private readonly double[] window;
        private readonly int frameShift;
        private readonly DftAction action;

        private Framing framing;
        private Complex[][] dftBuffer;

        public StftAnalysis(int channelCount, double[] window, int frameShift, DftAction action)
        {
            if (channelCount < 0) throw new ArgumentOutOfRangeException("The number of channels must be greater than or equal to zero.", nameof(channelCount));
            if (window == null) throw new ArgumentNullException(nameof(window));
            if (frameShift <= 0) throw new ArgumentOutOfRangeException("The frame shift must be greater than zero.", nameof(frameShift));
            if (frameShift > window.Length) throw new ArgumentOutOfRangeException("The frame shift must be less than or equal to the length of the window.");
            if (action == null) throw new ArgumentNullException(nameof(action));

            this.channelCount = channelCount;
            this.window = window.ToArray();
            this.frameShift = frameShift;
            this.action = action;

            framing = new Framing(channelCount, window.Length, frameShift, FrameAction);
            dftBuffer = new Complex[channelCount][];
            for (var ch = 0; ch < channelCount; ch++)
            {
                dftBuffer[ch] = new Complex[window.Length];
            }
        }

        public void Process(IDspContext context, float[][] frame, int length)
        {
            framing.Process(context, frame, length);
        }

        private void FrameAction(IDspContext context, long position, float[][] frame)
        {
            for (var ch = 0; ch < channelCount; ch++)
            {
                for (var t = 0; t < window.Length; t++)
                {
                    dftBuffer[ch][t] = window[t] * frame[ch][t];
                }
                Fourier.Forward(dftBuffer[ch], FourierOptions.AsymmetricScaling);
            }
            action(context, position, dftBuffer);
        }

        public long ProcessedSampleCount
        {
            get
            {
                return framing.ProcessedSampleCount;
            }
        }
    }



    public delegate void DftAction(IDspContext context, long position, Complex[][] dft);
}
