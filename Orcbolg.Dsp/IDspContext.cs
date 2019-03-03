using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orcbolg.Dsp
{
    public interface IDspContext
    {
        void Post(IDspCommand command);

        long ProcessedSampleCount { get; }
        Task Completion { get; }
    }
}
