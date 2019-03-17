using System;
using System.Collections.Generic;
using System.Linq;

namespace Orcbolg.Dsp
{
    public sealed class MessageCommand : IDspCommand
    {
        private string value;

        public MessageCommand(string value)
        {
            this.value = value;
        }

        public string Value => value;
    }

    public static class MessageCommandEx
    {
        public static void SendMessage(this IDspContext context, string value)
        {
            context.Post(new MessageCommand(value));
        }
    }
}
