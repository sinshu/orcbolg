using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Orcbolg.Dsp
{
    public sealed class Framing
    {
        private readonly int channelCount;
        private readonly int frameLength;
        private readonly int frameShift;
        private readonly FrameAction action;

        private readonly float[][] buffer;
        private readonly float[][] frame;
        private long processedSampleCount;
        private int writeCount;
        private int shiftCount;

        public Framing(int channelCount, int frameLength, int frameShift, FrameAction action)
        {
            if (channelCount <= 0) throw new ArgumentOutOfRangeException("The number of channels must be greater than or equal to one.", nameof(channelCount));
            if (frameLength <= 0) throw new ArgumentOutOfRangeException("The frame length must be greater than zero.", nameof(frameLength));
            if (frameShift <= 0) throw new ArgumentOutOfRangeException("The frame shift must be greater than zero.", nameof(frameShift));
            if (frameShift > frameLength) throw new ArgumentOutOfRangeException("The frame shift must be less than or equal to the frame length.");
            if (action == null) throw new ArgumentNullException(nameof(action));

            this.channelCount = channelCount;
            this.frameLength = frameLength;
            this.frameShift = frameShift;
            this.action = action;

            buffer = new float[channelCount][];
            frame = new float[channelCount][];
            for (var ch = 0; ch < channelCount; ch++)
            {
                buffer[ch] = new float[frameLength];
                frame[ch] = new float[frameLength];
            }
            processedSampleCount = 0;
            writeCount = 0;
            shiftCount = 0;
        }

        public void Process(IDspContext context, float[][] interval, int length)
        {
            for (var t = 0; t < length; t++)
            {
                for (var ch = 0; ch < channelCount; ch++)
                {
                    buffer[ch][writeCount] = interval[ch][t];
                }
                processedSampleCount++;
                writeCount++;
                if (writeCount == frameLength)
                {
                    writeCount = 0;
                }
                shiftCount++;
                if (shiftCount == frameShift)
                {
                    DoAction(context);
                    shiftCount = 0;
                }
            }
        }

        private void DoAction(IDspContext context)
        {
            for (var ch = 0; ch < channelCount; ch++)
            {
                Array.Copy(buffer[ch], writeCount, frame[ch], 0, frameLength - writeCount);
                Array.Copy(buffer[ch], 0, frame[ch], frameLength - writeCount, writeCount);
            }
            action(context, processedSampleCount - frameLength, frame);
        }

        public long ProcessedSampleCount
        {
            get
            {
                return processedSampleCount;
            }
        }
    }



    public delegate void FrameAction(IDspContext context, long position, float[][] frame);
}
