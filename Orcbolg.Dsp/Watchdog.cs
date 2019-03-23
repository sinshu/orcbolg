using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NAudio.Wave;

namespace Orcbolg.Dsp
{
    public class Watchdog : INonrealtimeDsp
    {
        private TimeSpan threshold;
        private TimeSpan previous;

        private object mutex;
        private double weight;
        private double dspTime;

        public Watchdog(IDspDriver driver)
        {
            var value = Math.Max(1.5 * driver.IntervalLength / driver.SampleRate, 0.01);
            threshold = TimeSpan.FromSeconds(value);
            previous = TimeSpan.FromDays(1);

            mutex = new object();
            weight = Math.Pow(10, -3 / ((double)driver.SampleRate / driver.IntervalLength));
            dspTime = 0;
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


            var newDspTime = (1 - weight) * dspTime + weight * (command.DspBufferEntry.DspEndTime - command.DspBufferEntry.DspStartTime).TotalSeconds;
            lock (mutex)
            {
                dspTime = newDspTime;
            }
        }

        public double DspTime
        {
            get
            {
                lock (mutex)
                {
                    return dspTime;
                }
            }
        }
    }



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
