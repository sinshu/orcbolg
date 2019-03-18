using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Orcbolg.Dsp
{
    internal sealed class DspBuffer
    {
        private readonly int inputChannelCount;
        private readonly int outputChannelCount;
        private readonly int intervalLength;
        private readonly int entryCount;

        private readonly DspBufferEntry[] entries;

        private long writePosition;
        private long readPosition;

        public DspBuffer(int inputChannelCount, int outputChannelCount, int intervalLength, int entryCount)
        {
            this.inputChannelCount = inputChannelCount;
            this.outputChannelCount = outputChannelCount;
            this.intervalLength = intervalLength;
            this.entryCount = entryCount;

            entries = new DspBufferEntry[entryCount];
            for (var i = 0; i < entryCount; i++)
            {
                entries[i] = new DspBufferEntry(inputChannelCount, outputChannelCount, intervalLength);
            }

            writePosition = 0;
            readPosition = 0;
        }

        public void Reset()
        {
            foreach (var entry in entries)
            {
                entry.SetReferenceCount(1);
                entry.Release();
            }

            writePosition = 0;
            readPosition = 0;
        }

        public DspBufferEntry StartWriting()
        {
            var entry = entries[writePosition % entryCount];
            if (!entry.Used)
            {
                entry.Reuse();
                return entry;
            }
            else
            {
                return null;
            }
        }

        public void EndWriting()
        {
            Interlocked.Increment(ref writePosition);
        }

        public DspBufferEntry Read()
        {
            if (readPosition == Interlocked.Read(ref writePosition))
            {
                return null;
            }
            else
            {
                var entry = entries[readPosition % entryCount];
                readPosition++;
                return entry;
            }
        }

        public int InputChannelCount
        {
            get
            {
                return inputChannelCount;
            }
        }

        public int OutputChannelCount
        {
            get
            {
                return outputChannelCount;
            }
        }

        public int IntervalLength
        {
            get
            {
                return intervalLength;
            }
        }

        public int EntryCount
        {
            get
            {
                return entryCount;
            }
        }
    }
}
