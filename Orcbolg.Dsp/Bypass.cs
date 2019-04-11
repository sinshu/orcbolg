using System;
using System.Collections.Generic;
using System.Linq;

namespace Orcbolg.Dsp
{
    public sealed class Bypass : IRealtimeDsp
    {
        private readonly int inputChannelCount;
        private readonly int outputChannelCount;
        private readonly int[] outputToInputChannel;

        public Bypass(IDspDriver driver)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));

            inputChannelCount = driver.InputChannelCount;
            outputChannelCount = driver.OutputChannelCount;
            outputToInputChannel = new int[outputChannelCount];
        }

        public int Process(float[][] inputInterval, float[][] outputInterval, int length)
        {
            for (var ch = 0; ch < outputInterval.Length; ch++)
            {
                var inputChannel = outputToInputChannel[ch];
                var inputBuffer = inputInterval[inputChannel];
                var outputBuffer = outputInterval[ch];
                for (var t = 0; t < length; t++)
                {
                    outputBuffer[t] = inputBuffer[t];
                }
            }

            return 0;
        }

        public void SetConnection(int inputChannel, int outputChannel)
        {
            if (!(0 <= inputChannel && inputChannel < inputChannelCount))
            {
                throw new ArgumentOutOfRangeException(nameof(inputChannel));
            }
            if (!(0 <= outputChannel && outputChannel < outputChannelCount))
            {
                throw new ArgumentOutOfRangeException(nameof(outputChannel));
            }

            outputToInputChannel[outputChannel] = inputChannel;
        }
    }
}
