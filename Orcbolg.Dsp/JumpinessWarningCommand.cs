using System;
using System.Collections.Generic;
using System.Linq;

namespace Orcbolg.Dsp
{
    public class JumpinessWarningCommand : IDspCommand
    {
        private readonly long position;

        public JumpinessWarningCommand(long position)
        {
            this.position = position;
        }

        public long Position => position;
    }
}
