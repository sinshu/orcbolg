using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace Orcbolg.Dsp
{
    public sealed class StftSynthesis
    {
        private readonly int inputChannelCount;
        private readonly int outputChannelCount;
        private readonly double[] window;
        private readonly int frameShift;
        private readonly StftFunc func;

        private OverlapAdd overlapAdd;
        private Complex[][] inputBuffer;
        private Complex[][] outputBuffer;

        public StftSynthesis(int inputChannelCount, int outputChannelCount, double[] window, int frameShift, StftFunc func)
        {
            if (inputChannelCount == 0 && outputChannelCount == 0) throw new ArgumentException("At least one input or output channel must be specified.");
            if (inputChannelCount < 0) throw new ArgumentException("Number of input channels must be greater than or equal to zero.", nameof(inputChannelCount));
            if (outputChannelCount < 0) throw new ArgumentException("Number of output channels must be greater than or equal to zero.", nameof(outputChannelCount));
            if (window == null) throw new ArgumentNullException(nameof(window));
            if (frameShift <= 0) throw new ArgumentException("Frame shift must be greater than zero.", nameof(frameShift));
            if (frameShift > window.Length) throw new ArgumentException("Frame shift must be less than or equal to window length.");
            if (func == null) throw new ArgumentNullException(nameof(func));

            this.inputChannelCount = inputChannelCount;
            this.outputChannelCount = outputChannelCount;
            this.window = window.ToArray();
            this.frameShift = frameShift;
            this.func = func;

            overlapAdd = new OverlapAdd(inputChannelCount, outputChannelCount, window.Length, frameShift, FrameFunc);
            inputBuffer = new Complex[inputChannelCount][];
            for (var ch = 0; ch < inputChannelCount; ch++)
            {
                inputBuffer[ch] = new Complex[window.Length];
            }
            outputBuffer = new Complex[outputChannelCount][];
            for (var ch = 0; ch < outputChannelCount; ch++)
            {
                outputBuffer[ch] = new Complex[window.Length];
            }
        }

        // Since FFT in Math.NET seems to do memory allocation,
        // this implementation might trouble if it is used in the audio thread
        // and the realtime audio buffer is very short.
        private void FrameFunc(long position, float[][] inputFrame, float[][] outputFrame)
        {
            for (var ch = 0; ch < inputChannelCount; ch++)
            {
                for (var t = 0; t < window.Length; t++)
                {
                    inputBuffer[ch][t] = window[t] * inputFrame[ch][t];
                }
                Fourier.Forward(inputBuffer[ch], FourierOptions.AsymmetricScaling);
            }
            func(position, inputBuffer, outputBuffer);
            for (var ch = 0; ch < outputChannelCount; ch++)
            {
                Fourier.Inverse(outputBuffer[ch], FourierOptions.AsymmetricScaling);
                for (var t = 0; t < window.Length; t++)
                {
                    outputFrame[ch][t] = (float)(window[t] * outputBuffer[ch][t]).Real;
                }
            }
        }

        public long ProcessedSampleCount
        {
            get
            {
                return overlapAdd.ProcessedSampleCount;
            }
        }
    }



    public delegate void StftFunc(long position, Complex[][] inputStft, Complex[][] outputStft);
}
