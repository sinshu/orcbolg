using System;
using System.Collections.Generic;
using System.Linq;

namespace Orcbolg.Dsp
{
    public interface IRealtimeDsp
    {
        void Process(float[][] inputInterval, float[][] outputInterval, int length);
    }
}
