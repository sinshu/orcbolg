using System;
using System.Collections.Generic;
using System.Linq;

namespace Orcbolg.Dsp
{
    public class IntervalCommand : IDspCommand
    {
        private readonly DspBufferEntry entry;
        private readonly int length;

        internal IntervalCommand(DspBufferEntry entry, int length)
        {
            this.entry = entry;
            this.length = length;
        }

        public long Position => entry.Position;
        public float[][] InputInterval => entry.InputInterval;
        public float[][] OutputInterval => entry.OutputInterval;
        public int Length => length;
        internal DspBufferEntry DspBufferEntry => entry;
    }
}
