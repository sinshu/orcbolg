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

        private Setting setting;
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
                setting = await Task.Run(() => new Setting());
                dspComponent = new DspComponent(setting, this);
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

        private void btnRecordingStartStop_Click(object sender, EventArgs e)
        {
            recordingState?.ToggleRecording();
        }

        private void btnRecordingNumberDecrement_Click(object sender, EventArgs e)
        {
            recordingState?.DecrementNumber();
        }

        private void btnRecordingNumberIncrement_Click(object sender, EventArgs e)
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
            dspComponent?.WaveformMonitor.Resize();
        }

        private void ClosingStart()
        {
            dspComponent?.DspContext.Stop();
        }

        private void ClosingEnd()
        {
            dspComponent?.Dispose();
        }



        private class DspComponent : IDisposable
        {
            private AsioDspSetting asioDspSetting;
            private IDspDriver dspDriver;
            private Bypass bypass;
            private WaveformMonitor waveformMonitor;
            private Recorder recorder;
            private Watchdog watchdog;
            private CommandPicker commandPicker;
            private IDspContext dspContext;

            public DspComponent(Setting setting, MainForm form)
            {
                try
                {
                    asioDspSetting = new AsioDspSetting(GetActualDriverName(setting.DriverName), setting.SampleRate, setting.BufferLength);
                    foreach (var ch in setting.InputChannels)
                    {
                        asioDspSetting.InputChannels.Add(ch);
                    }
                    foreach (var ch in setting.OutputChannels)
                    {
                        asioDspSetting.OutputChannels.Add(ch);
                    }

                    dspDriver = new AsioDspDriver(asioDspSetting);
                    bypass = new Bypass(dspDriver);
                    dspDriver.AddDsp(bypass);
                    waveformMonitor = new WaveformMonitor(dspDriver, form.picWaveformMonitor, setting.UpdateCycle, false);
                    dspDriver.AddDsp(waveformMonitor);
                    recorder = new Recorder(dspDriver);
                    dspDriver.AddDsp(recorder);
                    watchdog = new Watchdog(dspDriver);
                    dspDriver.AddDsp(watchdog);
                    commandPicker = new CommandPicker(form);
                    dspDriver.AddDsp(commandPicker);

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
                if (waveformMonitor != null)
                {
                    waveformMonitor.Dispose();
                    waveformMonitor = null;
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

            public AsioDspSetting AsioDspSetting
            {
                get
                {
                    return asioDspSetting;
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

            public WaveformMonitor WaveformMonitor
            {
                get
                {
                    return waveformMonitor;
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
                form.dspComponent.WaveformMonitor.SetChannelFocus(channelFocus);

                UpdateForm();
            }

            private void UpdateForm()
            {
                form.lblRecordingNumber.Text = number.ToString();
                if (recording)
                {
                    form.pnlBottom.BackColor = Color.LightPink;
                    form.btnRecordingStartStop.BackColor = Color.LightPink;
                    form.btnRecordingStartStop.ForeColor = Color.DarkRed;
                    form.btnRecordingStartStop.Text = "■停止";
                    form.btnRecordingNumberDecrement.BackColor = Color.LightPink;
                    form.btnRecordingNumberDecrement.ForeColor = Color.DarkRed;
                    form.btnRecordingNumberIncrement.BackColor = Color.LightPink;
                    form.btnRecordingNumberIncrement.ForeColor = Color.DarkRed;
                    form.lblRecordingNumber.BackColor = Color.DarkRed;
                    form.lblRecordingNumber.ForeColor = Color.LightPink;
                    form.txtDebugInfo.BackColor = Color.LightPink;
                    form.txtDebugInfo.ForeColor = Color.DarkRed;
                }
                else
                {
                    form.pnlBottom.BackColor = Color.Empty;
                    form.btnRecordingStartStop.BackColor = Color.Empty;
                    form.btnRecordingStartStop.ForeColor = Color.Empty;
                    form.btnRecordingStartStop.Text = "●録音";
                    form.btnRecordingNumberDecrement.BackColor = Color.Empty;
                    form.btnRecordingNumberDecrement.ForeColor = Color.Empty;
                    form.btnRecordingNumberIncrement.BackColor = Color.Empty;
                    form.btnRecordingNumberIncrement.ForeColor = Color.Empty;
                    form.lblRecordingNumber.BackColor = SystemColors.ControlDarkDark;
                    form.lblRecordingNumber.ForeColor = SystemColors.ControlLight;
                    form.txtDebugInfo.BackColor = Color.Empty;
                    form.txtDebugInfo.ForeColor = Color.Empty;
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
                    AbortRecording();
                }
            }

            public void StartRecording()
            {
                if (!recording)
                {
                    if (Directory.Exists(form.setting.RecordingDirectory))
                    {
                        var path = Path.Combine(form.setting.RecordingDirectory, GetNewFileName() + ".wav");
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

            public void AbortRecording()
            {
                if (recording)
                {
                    form.dspComponent.DspContext.AbortRecording();
                    recording = false;
                    number = Math.Min(number + 1, 9999);
                    UpdateForm();
                }
            }

            public void OnRecordingStop()
            {
                if (recording)
                {
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
                form.dspComponent.WaveformMonitor.SetChannelFocus(channelFocus);
                form.picWaveformMonitor.Refresh();
            }

            public void DownChannelFocus()
            {
                channelFocus = Math.Min(channelFocus + 1, form.dspComponent.DspDriver.InputChannelCount - 1);
                form.dspComponent.WaveformMonitor.SetChannelFocus(channelFocus);
                form.picWaveformMonitor.Refresh();
            }
        }



        private class CommandPicker : INonrealtimeDsp
        {
            private MainForm form;

            public CommandPicker(MainForm form)
            {
                this.form = form;
            }

            public void Process(IDspContext context, IDspCommand command)
            {
                var recordingStopCommand = command as RecordingStopCommand;
                if (recordingStopCommand != null)
                {
                    Process(context, recordingStopCommand);
                }
            }

            public void Process(IDspContext context, RecordingStopCommand command)
            {
                form.Invoke((MethodInvoker)(() => form.recordingState.OnRecordingStop()));
            }
        }
    }
}
