using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
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

        private void btnRecord_Click(object sender, EventArgs e)
        {
            recordingState?.ToggleRecording();
        }

        private void btnNumberDecrement_Click(object sender, EventArgs e)
        {
            recordingState?.DecrementNumber();
        }

        private void btnNumberIncrement_Click(object sender, EventArgs e)
        {
            recordingState?.IncrementNumber();
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyData)
            {
                case Keys.Space:
                    recordingState?.ToggleRecording();
                    break;
                case Keys.Oemcomma:
                    recordingState?.DecrementNumber();
                    break;
                case Keys.OemPeriod:
                    recordingState?.IncrementNumber();
                    break;
                case Keys.OemOpenBrackets:
                    recordingState?.UpChannelFocus();
                    break;
                case Keys.Oem6:
                    recordingState?.DownChannelFocus();
                    break;
                default:
                    dspComponent?.DspContext.OnKeyDown(e.KeyCode.ToString());
                    break;
            }
            e.Handled = true;
        }

        private void MonitorResize()
        {
            dspComponent?.Monitor.Resize();
        }

        private void ClosingStart()
        {
            dspComponent?.DspContext.Stop();
        }

        private void ClosingEnd()
        {
            dspComponent?.Dispose();
        }



        private class RecordingState
        {
            private MainForm form;

            private int number;
            private bool recording;

            private int channelFocus;

            public RecordingState(MainForm form)
            {
                this.form = form;

                number = 1;
                recording = false;

                channelFocus = 0;

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
                    if (Directory.Exists(form.appSetting.RecordingDirectory))
                    {
                        var path = Path.Combine(form.appSetting.RecordingDirectory, GetNewFileName() + ".wav");
                        form.dspComponent.DspContext.StartRecording(number, path);
                        recording = true;
                        UpdateForm();
                    }
                    else
                    {
                        Console.WriteLine("ｳﾜｧｰ");
                    }
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

            public void UpChannelFocus()
            {
                channelFocus = Math.Max(channelFocus - 1, 0);
                form.dspComponent.Monitor.SetChannelFocus(channelFocus);
                form.picMonitor.Refresh();
            }

            public void DownChannelFocus()
            {
                channelFocus = Math.Min(channelFocus + 1, form.dspComponent.DspDriver.InputChannelCount - 1);
                form.dspComponent.Monitor.SetChannelFocus(channelFocus);
                form.picMonitor.Refresh();
            }
        }
    }
}
