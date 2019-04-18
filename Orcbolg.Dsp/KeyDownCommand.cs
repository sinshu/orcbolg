using System;
using System.Collections.Generic;
using System.Linq;

namespace Orcbolg.Dsp
{
    public sealed class KeyDownCommand : IDspCommand
    {
        private string value;

        public KeyDownCommand(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            this.value = value;
        }

        public string Value
        {
            get
            {
                return value;
            }
        }
    }



    public static class KeyDownCommandEx
    {
        public static void SendKeyDownEvent(this IDspContext context, string value)
        {
            context.Post(new KeyDownCommand(value));
        }
    }
}
