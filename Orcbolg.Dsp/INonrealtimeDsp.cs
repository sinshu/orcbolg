using System;
using System.Collections.Generic;
using System.Linq;

namespace Orcbolg.Dsp
{
    public interface INonrealtimeDsp
    {
        void Process(IDspContext context, IDspCommand command);
    }
}
