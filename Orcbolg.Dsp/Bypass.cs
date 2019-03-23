using System;
using System.Collections.Generic;
using System.Linq;

namespace Orcbolg.Dsp
{
    public class Bypass : IRealtimeDsp
    {
        private int[] outputToInputChannel;

        public Bypass(IDspDriver driver)
        {
            outputToInputChannel = new int[driver.OutputChannelCount];
        }

        public void Process(float[][] inputBuffers, float[][] outputBuffers, int length)
        {
            for (var ch = 0; ch < outputBuffers.Length; ch++)
            {
                var inputChannel = outputToInputChannel[ch];
                var inputBuffer = inputBuffers[inputChannel];
                var outputBuffer = outputBuffers[ch];
                for (var t = 0; t < length; t++)
                {
                    outputBuffer[t] = inputBuffer[t];
                }
            }
        }

        public void SetConnection(int inputChannel, int outputChannel)
        {
            outputToInputChannel[outputChannel] = inputChannel;
        }
    }
}
