﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Orcbolg.Dsp
{
    public sealed class OverlapAdd
    {
        private readonly int inputChannelCount;
        private readonly int outputChannelCount;
        private readonly int frameLength;
        private readonly int frameShift;
        private readonly FrameFunc func;

        private readonly int overlapLength;

        private readonly float[][] inputBuffer;
        private readonly float[][] inputFrame;
        private readonly float[][] outputBuffer;
        private readonly float[][] outputFrame;
        private long processedSampleCount;
        private int inputWriteCount;
        private int outputAddPosition;
        private int shiftCount;

        public OverlapAdd(int inputChannelCount, int outputChannelCount, int frameLength, int frameShift, FrameFunc func)
        {
            if (inputChannelCount <= 0) throw new ArgumentOutOfRangeException("The number of input channels must be greater than or equal to one.", nameof(inputChannelCount));
            if (outputChannelCount <= 0) throw new ArgumentOutOfRangeException("The number of output channels must be greater than or equal to one.", nameof(outputChannelCount));
            if (frameLength <= 0) throw new ArgumentOutOfRangeException("The frame length must be greater than zero.", nameof(frameLength));
            if (frameShift <= 0) throw new ArgumentOutOfRangeException("The frame shift must be greater than zero.", nameof(frameShift));
            if (frameShift > frameLength) throw new ArgumentOutOfRangeException("The frame shift must be less than or equal to the frame length.");
            if (func == null) throw new ArgumentNullException(nameof(func));

            this.inputChannelCount = inputChannelCount;
            this.outputChannelCount = outputChannelCount;
            this.frameLength = frameLength;
            this.frameShift = frameShift;
            this.func = func;

            overlapLength = frameLength - frameShift;

            inputBuffer = new float[inputChannelCount][];
            inputFrame = new float[inputChannelCount][];
            outputBuffer = new float[outputChannelCount][];
            outputFrame = new float[outputChannelCount][];
            for (var ch = 0; ch < inputChannelCount; ch++)
            {
                inputBuffer[ch] = new float[frameLength];
                inputFrame[ch] = new float[frameLength];
            }
            for (var ch = 0; ch < outputChannelCount; ch++)
            {
                outputBuffer[ch] = new float[frameLength];
                outputFrame[ch] = new float[frameLength];
            }
            processedSampleCount = 0;
            inputWriteCount = 0;
            outputAddPosition = 0;
            shiftCount = 0;
        }

        public void Process(float[][] inputInterval, float[][] outputInterval, int length)
        {
            for (var t = 0; t < length; t++)
            {
                for (var ch = 0; ch < inputChannelCount; ch++)
                {
                    inputBuffer[ch][inputWriteCount] = inputInterval[ch][t];
                }
                var u = (outputAddPosition + shiftCount) % frameLength;
                for (var ch = 0; ch < outputChannelCount; ch++)
                {
                    outputInterval[ch][t] = outputBuffer[ch][u];
                }
                processedSampleCount++;
                inputWriteCount++;
                if (inputWriteCount == frameLength)
                {
                    inputWriteCount = 0;
                }
                shiftCount++;
                if (shiftCount == frameShift)
                {
                    outputAddPosition = (outputAddPosition + frameShift) % frameLength;
                    DoFunc();
                    shiftCount = 0;
                }
            }
        }

        private void DoFunc()
        {
            for (var ch = 0; ch < inputChannelCount; ch++)
            {
                Array.Copy(inputBuffer[ch], inputWriteCount, inputFrame[ch], 0, frameLength - inputWriteCount);
                Array.Copy(inputBuffer[ch], 0, inputFrame[ch], frameLength - inputWriteCount, inputWriteCount);
            }
            func(processedSampleCount - frameLength, inputFrame, outputFrame);
            for (var t = 0; t < overlapLength; t++)
            {
                var u = (outputAddPosition + t) % frameLength;
                for (var ch = 0; ch < outputChannelCount; ch++)
                {
                    outputBuffer[ch][u] += outputFrame[ch][t];
                }
            }
            for (var t = overlapLength; t < frameLength; t++)
            {
                var u = (outputAddPosition + t) % frameLength;
                for (var ch = 0; ch < outputChannelCount; ch++)
                {
                    outputBuffer[ch][u] = outputFrame[ch][t];
                }
            }
        }

        public long ProcessedSampleCount
        {
            get
            {
                return processedSampleCount;
            }
        }
    }



    public delegate void FrameFunc(long position, float[][] inputFrame, float[][] outputFrame);
}
