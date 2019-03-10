using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orcbolg.Dsp
{
    public interface IDspDriver : IDisposable
    {
        void AddDsp(IRealtimeDsp dsp);
        void AddDsp(INonrealtimeDsp dsp);
        IDspContext Run();
        string GetInputChannelName(int channel);
        string GetOutputChannelName(int channel);

        string DriverName { get; }
        int SampleRate { get; }
        int InputChannelCount { get; }
        int OutputChannelCount { get; }
        int IntervalLength { get; }
    }
}
