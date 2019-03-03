using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Orcbolg.Dsp
{
    internal class DspBufferEntry
    {
        private readonly float[][] inputInterval;
        private readonly float[][] outputInterval;

        private bool used;
        private int referenceCount;

        private long position;
        private TimeSpan dspStartTime;
        private TimeSpan dspEndTime;
        private DateTime globalTime;

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

        public float[][] InputInterval => inputInterval;
        public float[][] OutputInterval => outputInterval;
        public bool Used => used;

        public long Position
        {
            get => position;
            set => position = value;
        }

        public TimeSpan DspStartTime
        {
            get => dspStartTime;
            set => dspStartTime = value;
        }

        public TimeSpan DspEndTime
        {
            get => dspEndTime;
            set => dspEndTime = value;
        }

        public DateTime GlobalTime
        {
            get => globalTime;
            set => globalTime = value;
        }
    }
}
