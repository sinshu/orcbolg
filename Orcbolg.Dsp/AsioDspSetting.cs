using System;
using System.Collections.Generic;
using System.Linq;

namespace Orcbolg.Dsp
{
    public sealed class AsioDspSetting
    {
        private readonly string driverName;
        private readonly int sampleRate;
        private readonly int bufferLength;
        private readonly List<int> inputChannels;
        private readonly List<int> outputChannels;
        private bool useLongInterval;

        public AsioDspSetting(string driverName, int sampleRate, int bufferLength)
        {
            if (driverName == null) throw new ArgumentNullException(nameof(driverName));
            if (sampleRate <= 0) throw new ArgumentException("Sample rate must be greater than zero.");
            if (bufferLength <= 0) throw new ArgumentException("Buffer length must be greater than zero.");

            this.driverName = driverName;
            this.sampleRate = sampleRate;
            this.bufferLength = bufferLength;
            inputChannels = new List<int>();
            outputChannels = new List<int>();
            useLongInterval = true;
        }

        public string DriverName
        {
            get
            {
                return driverName;
            }
        }

        public int SampleRate
        {
            get
            {
                return sampleRate;
            }
        }

        public int BufferLength
        {
            get
            {
                return bufferLength;
            }
        }

        public IList<int> InputChannels
        {
            get
            {
                return inputChannels;
            }
        }

        public IList<int> OutputChannels
        {
            get
            {
                return outputChannels;
            }
        }

        public bool UseLongInterval
        {
            get
            {
                return useLongInterval;
            }

            set
            {
                useLongInterval = value;
            }
        }
    }
}
