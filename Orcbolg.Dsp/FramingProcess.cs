using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Orcbolg.Dsp
{
    public sealed class FramingProcess
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

        public FramingProcess(int channelCount, int frameLength, int frameShift, FrameAction action)
        {
            if (channelCount <= 0) throw new ArgumentException("Number of channels must be greater than or equal to one.");
            if (frameLength <= 0) throw new ArgumentException("Frame length must be greater than or equal to one.");
            if (frameShift <= 0) throw new ArgumentException("Frame shift must be greater than or equal to one.");
            if (frameShift > frameLength) throw new ArgumentException("Frame shift must be less than or equal to frame length.");
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

        public void Process(float[][] interval, int length)
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
                    DoAction();
                    shiftCount = 0;
                }
            }
        }

        private void DoAction()
        {
            for (var ch = 0; ch < channelCount; ch++)
            {
                Array.Copy(buffer[ch], writeCount, frame[ch], 0, frameLength - writeCount);
                Array.Copy(buffer[ch], 0, frame[ch], frameLength - writeCount, writeCount);
            }
            action(processedSampleCount - frameLength, frame);
        }

        public long ProcessedSampleCount
        {
            get
            {
                return processedSampleCount;
            }
        }
    }



    public delegate void FrameAction(long position, float[][] frame);
}
