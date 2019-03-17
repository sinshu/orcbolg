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
    internal partial class MainForm : Form
    {
        private bool running;
        private AppSetting appSetting;
        private DspComponent dspComponent;
        private RecordingState recordingState;

        public MainForm()
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
                dspComponent = new DspComponent(appSetting, picMonitor);
                FormHelper.SetFormResizeAction(this, MonitorResize);
                FormHelper.SetAsyncFormClosingAction(this, ClosingStart, ClosingEnd, dspComponent.DspContext.Completion);
                recordingState = new RecordingState(this);
                await dspComponent.DspContext.Completion;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (recordingState != null)
            {
                recordingState.ToggleRecording();
            }
        }

        private void btnNumberDecrement_Click(object sender, EventArgs e)
        {
            if (recordingState != null)
            {
                recordingState.DecrementNumber();
            }
        }

        private void btnNumberIncrement_Click(object sender, EventArgs e)
        {
            if (recordingState != null)
            {
                recordingState.IncrementNumber();
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Space)
            {
                if (recordingState != null)
                {
                    recordingState.ToggleRecording();
                }
                e.Handled = true;
            }
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



        private class RecordingState
        {
            private MainForm form;

            private int number;
            private bool recording;

            public RecordingState(MainForm form)
            {
                this.form = form;

                number = 1;
                recording = false;

                UpdateForm();
            }

            private void UpdateForm()
            {
                form.lblNumber.Text = number.ToString();
                if (recording)
                {
                    form.pnlBottom.BackColor = Color.LightPink;
                    form.btnRecord.BackColor = Color.LightPink;
                    form.btnRecord.ForeColor = Color.DarkRed;
                    form.btnRecord.Text = "■停止";
                    form.btnNumberDecrement.BackColor = Color.LightPink;
                    form.btnNumberDecrement.ForeColor = Color.DarkRed;
                    form.btnNumberIncrement.BackColor = Color.LightPink;
                    form.btnNumberIncrement.ForeColor = Color.DarkRed;
                    form.lblNumber.BackColor = Color.DarkRed;
                    form.lblNumber.ForeColor = Color.LightPink;
                    form.txtDebug.BackColor = Color.LightPink;
                    form.txtDebug.ForeColor = Color.DarkRed;
                }
                else
                {
                    form.pnlBottom.BackColor = Color.Empty;
                    form.btnRecord.BackColor = Color.Empty;
                    form.btnRecord.ForeColor = Color.Empty;
                    form.btnRecord.Text = "●録音";
                    form.btnNumberDecrement.BackColor = Color.Empty;
                    form.btnNumberDecrement.ForeColor = Color.Empty;
                    form.btnNumberIncrement.BackColor = Color.Empty;
                    form.btnNumberIncrement.ForeColor = Color.Empty;
                    form.lblNumber.BackColor = SystemColors.ControlDarkDark;
                    form.lblNumber.ForeColor = SystemColors.ControlLight;
                    form.txtDebug.BackColor = Color.Empty;
                    form.txtDebug.ForeColor = Color.Empty;
                }
            }

            public void ToggleRecording()
            {
                if (!recording)
                {
                    StartRecording();
                }
                else
                {
                    StopRecording();
                }
            }

            public void StartRecording()
            {
                if (!recording)
                {
                    var path = GetNewFileName() + ".wav";
                    form.dspComponent.DspContext.StartRecording(number, path);
                    recording = true;
                    UpdateForm();
                }
            }

            public void StopRecording()
            {
                if (recording)
                {
                    form.dspComponent.DspContext.StopRecording(number);
                    recording = false;
                    number = Math.Min(number + 1, 9999);
                    UpdateForm();
                }
            }

            public void DecrementNumber()
            {
                number = Math.Max(number - 1, 1);
                UpdateForm();
            }

            public void IncrementNumber()
            {
                number = Math.Min(number + 1, 9999);
                UpdateForm();
            }

            private string GetNewFileName()
            {
                return DateTime.Now.ToString("yyyyMMdd_HHmmss_") + number.ToString("0000");
            }
        }
    }
}
