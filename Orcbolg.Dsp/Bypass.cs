using System;
using System.Collections.Generic;
using System.Linq;

namespace Orcbolg.Dsp
{
    public class Bypass : IRealtimeDsp
    {
        public Bypass(IDspDriver driver)
        {
        }

        public void Process(float[][] inputBuffers, float[][] outputBuffers, int length)
        {
            for (var ch = 0; ch < outputBuffers.Length; ch++)
            {
                for (var t = 0; t < length; t++)
                {
                    var value = inputBuffers[0][t];
                    outputBuffers[ch][t] = value;
                }
            }
        }
    }
}
