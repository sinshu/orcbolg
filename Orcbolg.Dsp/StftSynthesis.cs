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
        private readonly DftFunc func;

        private readonly double scale;

        private OverlapAdd overlapAdd;
        private Complex[][] inputDftBuffer;
        private Complex[][] outputDftBuffer;

        public StftSynthesis(int inputChannelCount, int outputChannelCount, double[] window, int frameShift, DftFunc func)
        {
            if (inputChannelCount == 0 && outputChannelCount == 0) throw new ArgumentException("At least one input or output channel must be specified.");
            if (inputChannelCount < 0) throw new ArgumentOutOfRangeException("The number of input channels must be greater than or equal to zero.", nameof(inputChannelCount));
            if (outputChannelCount < 0) throw new ArgumentOutOfRangeException("The number of output channels must be greater than or equal to zero.", nameof(outputChannelCount));
            if (window == null) throw new ArgumentNullException(nameof(window));
            if (frameShift <= 0) throw new ArgumentOutOfRangeException("The frame shift must be greater than zero.", nameof(frameShift));
            if (frameShift > window.Length) throw new ArgumentOutOfRangeException("The frame shift must be less than or equal to the length of the window.");
            if (func == null) throw new ArgumentNullException(nameof(func));

            this.inputChannelCount = inputChannelCount;
            this.outputChannelCount = outputChannelCount;
            this.window = window.ToArray();
            this.frameShift = frameShift;
            this.func = func;

            scale = 1 / (window.Select(x => x * x).Average() * ((double)window.Length / frameShift));

            overlapAdd = new OverlapAdd(inputChannelCount, outputChannelCount, window.Length, frameShift, FrameFunc);
            inputDftBuffer = new Complex[inputChannelCount][];
            for (var ch = 0; ch < inputChannelCount; ch++)
            {
                inputDftBuffer[ch] = new Complex[window.Length];
            }
            outputDftBuffer = new Complex[outputChannelCount][];
            for (var ch = 0; ch < outputChannelCount; ch++)
            {
                outputDftBuffer[ch] = new Complex[window.Length];
            }
        }

        public void Process(float[][] inputInterval, float[][] outputInterval, int length)
        {
            overlapAdd.Process(inputInterval, outputInterval, length);
        }

        // Since the FFT implementation in Math.NET seems to do memory allocation,
        // this code might trouble if it is used in the audio thread
        // and the realtime audio buffer is very short.
        private void FrameFunc(long position, float[][] inputFrame, float[][] outputFrame)
        {
            for (var ch = 0; ch < inputChannelCount; ch++)
            {
                for (var t = 0; t < window.Length; t++)
                {
                    inputDftBuffer[ch][t] = window[t] * inputFrame[ch][t];
                }
                Fourier.Forward(inputDftBuffer[ch], FourierOptions.AsymmetricScaling);
            }
            func(position, inputDftBuffer, outputDftBuffer);
            for (var ch = 0; ch < outputChannelCount; ch++)
            {
                Fourier.Inverse(outputDftBuffer[ch], FourierOptions.AsymmetricScaling);
                for (var t = 0; t < window.Length; t++)
                {
                    outputFrame[ch][t] = (float)(scale * window[t] * outputDftBuffer[ch][t]).Real;
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



    public delegate void DftFunc(long position, Complex[][] inputDft, Complex[][] outputDft);
}
