﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Orcbolg.Dsp
{
    public sealed class StopCommand : IDspCommand
    {
        private readonly Exception exception;

        public StopCommand()
        {
            exception = null;
        }

        internal StopCommand(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            this.exception = exception;
        }

        internal Exception Exception
        {
            get
            {
                return exception;
            }
        }
    }

    public static class StopCommandEx
    {
        public static void Stop(this IDspContext context)
        {
            context.Post(new StopCommand());
        }

        internal static void Stop(this IDspContext context, Exception exception)
        {
            context.Post(new StopCommand(exception));
        }
    }
}
