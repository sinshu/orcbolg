using System;
using System.Collections.Generic;
using System.Linq;

namespace Orcbolg.Dsp
{
    public class DspException : Exception
    {
        internal DspException(string message) : base(message)
        {
        }

        internal DspException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
