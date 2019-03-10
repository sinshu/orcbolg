using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;

namespace Orcbolg.Dsp
{
    public class ExceptionTest : INonrealtimeDsp
    {
        private int count;

        public ExceptionTest()
        {
            count = 0;
        }

        public void Process(IDspContext context, IDspCommand command)
        {
            System.Threading.Thread.Sleep(10);

            //System.Threading.Thread.Sleep(300);
            count++;

            if (count == 100)
            {
                //throw new Exception("おわああ");
            }

            var interval = command as IntervalCommand;
            if (interval != null)
            {
                //Console.WriteLine(interval.Position);
            }
        }
    }
}
