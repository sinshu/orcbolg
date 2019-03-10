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
        IDspDriver driver;
        IDspContext context;

        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Shown(object sender, EventArgs e)
        {
            var setting = new AsioDspSetting("ASIO4ALL v2", 48000, 1 * 48000);
            setting.InputChannels.Add(0);
            setting.InputChannels.Add(1);
            setting.OutputChannels.Add(0);
            setting.OutputChannels.Add(1);
            //driver = new AsioDspDriver(setting);
            //driver = new FileDspDriver("test_dsp.wav", 4567);
            driver = new FileDspDriver("test_dsp.wav", 4567, "output.wav", 2);
            var rec = new RecordTest(driver);
            var test = new ExceptionTest();
            var bypass = new BypassDsp(driver);
            var monitor = new WaveformMonitorDsp(driver, pictureBox1, 1024, true);
            driver.AddDsp(rec);
            driver.AddDsp(test);
            driver.AddDsp(bypass);
            driver.AddDsp(monitor);

            context = driver.Run();
            FormHelper.SetAsyncFormClosingAction(this, ClosingStart, ClosingEnd, context.Completion);
            try
            {
                await context.Completion;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            rec.Dispose();
            driver.Dispose();
            Console.WriteLine("STOPEED!");
        }

        private void button1_Click(object sender, EventArgs e)
        {
        }

        private void ClosingStart()
        {
            context.Stop();
        }

        private void ClosingEnd()
        {
            driver.Dispose();
        }
    }
}
