using System;
using System.Collections.Generic;
using System.Linq;

namespace Orcbolg.Dsp
{
    public class InputGain : IRealtimeDsp
    {
        private float[] gains;

        public InputGain(IDspDriver driver, IReadOnlyList<float> gains)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (gains.Count != driver.InputChannelCount) throw new ArgumentException("Number of gains must be equal to number of input channels.");

            this.gains = gains.ToArray();
        }

        public int Process(float[][] inputBuffers, float[][] outputBuffers, int length)
        {
            for (var ch = 0; ch < inputBuffers.Length; ch++)
            {
                var gain = gains[ch];
                var buffer = inputBuffers[ch];
                for (var t = 0; t < length; t++)
                {
                    buffer[t] *= gain;
                }
            }

            return 0;
        }
    }
}
