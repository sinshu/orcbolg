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
        private BypassDsp bypass;
        private WaveformMonitor monitor;
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
                bypass = new BypassDsp(dspDriver);
                dspDriver.AddDsp(bypass);
                monitor = new WaveformMonitor(dspDriver, pictureBox, 2048, true);
                dspDriver.AddDsp(monitor);

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

        public AsioDspSetting DspSetting => dspSetting;
        public IDspDriver DspDriver => dspDriver;
        public BypassDsp Bypass => bypass;
        public WaveformMonitor Monitor => monitor;
        public IDspContext DspContext => dspContext;
    }
}
