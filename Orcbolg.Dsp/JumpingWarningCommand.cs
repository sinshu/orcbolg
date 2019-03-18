using System;
using System.Collections.Generic;
using System.Linq;

namespace Orcbolg.Dsp
{
    public class JumpingWarningCommand : IDspCommand
    {
        private readonly long position;

        public JumpingWarningCommand(long position)
        {
            this.position = position;
        }

        public long Position
        {
            get
            {
                return position;
            }
        }
    }
}
