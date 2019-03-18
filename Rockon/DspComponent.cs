using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Orcbolg.Dsp;

namespace Rockon
{
    internal class DspComponent : IDisposable
    {
        private AsioDspSetting dspSetting;
        private IDspDriver dspDriver;
        private Bypass bypass;
        private WaveformMonitor monitor;
        private Recorder recorder;
        private Watchdog watchdog;
        private IDspContext dspContext;

        public DspComponent(AppSetting appSetting, PictureBox pictureBox)
        {
            try
            {
                dspSetting = new AsioDspSetting(GetActualDriverName(appSetting.DriverName), appSetting.SampleRate, appSetting.BufferLength);
                foreach (var ch in appSetting.InputChannels)
                {
                    dspSetting.InputChannels.Add(ch);
                }
                foreach (var ch in appSetting.OutputChannels)
                {
                    dspSetting.OutputChannels.Add(ch);
                }

                dspDriver = new AsioDspDriver(dspSetting);
                //dspDriver = new FileDspDriver("test_dsp.wav", 123, "output.wav", 2);
                bypass = new Bypass(dspDriver);
                dspDriver.AddDsp(bypass);
                monitor = new WaveformMonitor(dspDriver, pictureBox, 2048, true);
                dspDriver.AddDsp(monitor);
                recorder = new Recorder(dspDriver);
                dspDriver.AddDsp(recorder);
                watchdog = new Watchdog(dspDriver);
                dspDriver.AddDsp(watchdog);

                dspContext = dspDriver.Run();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private string GetActualDriverName(string shortName)
        {
            foreach (var actualName in AsioDspDriver.EnumerateDriverNames())
            {
                if (actualName.ToLower().Contains(shortName.ToLower()))
                {
                    return actualName;
                }
            }
            throw new Exception("オーディオデバイス " + shortName + " が見つかりませんでした。");
        }

        public void Dispose()
        {
            if (recorder != null)
            {
                recorder.Dispose();
                recorder = null;
            }
            if (monitor != null)
            {
                monitor.Dispose();
                monitor = null;
            }
            if (dspContext != null)
            {
                dspContext.Stop();
                dspContext = null;
            }
            if (dspDriver != null)
            {
                dspDriver.Dispose();
                dspDriver = null;
            }
        }

        public AsioDspSetting DspSetting
        {
            get
            {
                return dspSetting;
            }
        }

        public IDspDriver DspDriver
        {
            get
            {
                return dspDriver;
            }
        }

        public Bypass Bypass
        {
            get
            {
                return bypass;
            }
        }

        public WaveformMonitor Monitor
        {
            get
            {
                return monitor;
            }
        }

        public Watchdog Watchdog
        {
            get
            {
                return watchdog;
            }
        }

        public IDspContext DspContext
        {
            get
            {
                return dspContext;
            }
        }
    }
}
