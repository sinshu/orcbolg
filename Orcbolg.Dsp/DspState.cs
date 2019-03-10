using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;

namespace Orcbolg.Dsp
{
    internal enum DspState
    {
        Initialized = 1,
        Running = 2,
        Stop = 3,
        Disposed = 4
    }
}
