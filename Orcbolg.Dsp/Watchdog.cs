﻿using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;

namespace Orcbolg.Dsp
{
    public class Watchdog : INonrealtimeDsp
    {
        private TimeSpan threshold;
        private TimeSpan previous;

        public Watchdog(IDspDriver driver)
        {
            var value = 1.5 * driver.IntervalLength / driver.SampleRate;
            threshold = TimeSpan.FromSeconds(value);
            previous = TimeSpan.FromDays(1);
        }

        public void Process(IDspContext context, IDspCommand command)
        {
            var intervalCommand = command as IntervalCommand;
            if (intervalCommand != null)
            {
                Process(context, intervalCommand);
            }
        }

        private void Process(IDspContext context, IntervalCommand command)
        {
            var current = command.DspBufferEntry.DspStartTime;
            var duration = current - previous;
            if (duration >= threshold)
            {
                context.Post(new JumpingWarningCommand(command.Position));
            }
            previous = current;
        }
    }
}
