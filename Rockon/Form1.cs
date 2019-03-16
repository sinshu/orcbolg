using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Orcbolg.Dsp;

namespace Rockon
{
    internal partial class Form1 : Form
    {
        private bool running;
        private AppSetting appSetting;
        private DspComponent dspComponent;

        public Form1()
        {
            InitializeComponent();

            running = false;
        }

        private async void Form1_Shown(object sender, EventArgs e)
        {
            if (running)
            {
                return;
            }

            running = true;

            try
            {
                appSetting = await Task.Run(() => new AppSetting());
                dspComponent = new DspComponent(appSetting, pictureBox1);
                FormHelper.SetFormResizeAction(this, MonitorResize);
                FormHelper.SetAsyncFormClosingAction(this, ClosingStart, ClosingEnd, dspComponent.DspContext.Completion);
                await dspComponent.DspContext.Completion;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
        }

        private void MonitorResize()
        {
            dspComponent.Monitor.Resize();
        }

        private void ClosingStart()
        {
            dspComponent.DspContext.Stop();
        }

        private void ClosingEnd()
        {
            dspComponent.Dispose();
        }
    }
}
