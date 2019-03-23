using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Orcbolg.Dsp
{
    internal sealed class DspBufferEntry
    {
        private readonly float[][] inputInterval;
        private readonly float[][] outputInterval;

        private bool used;
        private int referenceCount;

        private long position;
        private TimeSpan dspStartTime;
        private TimeSpan dspEndTime;

        public DspBufferEntry(int inputChannelCount, int outputChannelCount, int intervalLength)
        {
            inputInterval = new float[inputChannelCount][];
            for (var ch = 0; ch < inputChannelCount; ch++)
            {
                inputInterval[ch] = new float[intervalLength];
            }

            outputInterval = new float[outputChannelCount][];
            for (var ch = 0; ch < outputChannelCount; ch++)
            {
                outputInterval[ch] = new float[intervalLength];
            }

            used = false;
            referenceCount = 0;
        }

        public void Reuse()
        {
            used = true;
        }

        public void SetReferenceCount(int referenceCount)
        {
            this.referenceCount = referenceCount;
        }

        public void Release()
        {
            var newReferenceCount = Interlocked.Decrement(ref referenceCount);
            if (newReferenceCount == 0)
            {
                used = false;
            }
        }

        public float[][] InputInterval
        {
            get
            {
                return inputInterval;
            }
        }

        public float[][] OutputInterval
        {
            get
            {
                return outputInterval;
            }
        }

        public bool Used
        {
            get
            {
                return used;
            }
        }

        public long Position
        {
            get
            {
                return position;
            }

            set
            {
                position = value;
            }
        }

        public TimeSpan DspStartTime
        {
            get
            {
                return dspStartTime;
            }

            set
            {
                dspStartTime = value;
            }
        }

        public TimeSpan DspEndTime
        {
            get
            {
                return dspEndTime;
            }

            set
            {
                dspEndTime = value;
            }
        }
    }
}
