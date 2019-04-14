using System;
using System.Collections.Generic;
using System.Linq;

namespace Orcbolg.Dsp
{
    public class OutputGain : IRealtimeDsp
    {
        private float[] gains;

        public OutputGain(IDspDriver driver, IReadOnlyList<float> gains)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (gains == null) throw new ArgumentNullException(nameof(gains));
            if (gains.Count != driver.OutputChannelCount) throw new ArgumentException("Number of gains must be equal to number of output channels.", nameof(gains));

            this.gains = gains.ToArray();
        }

        public int Process(float[][] inputInterval, float[][] outputInterval, int length)
        {
            for (var ch = 0; ch < outputInterval.Length; ch++)
            {
                var gain = gains[ch];
                var buffer = outputInterval[ch];
                for (var t = 0; t < length; t++)
                {
                    buffer[t] *= gain;
                }
            }

            return 0;
        }
    }
}
