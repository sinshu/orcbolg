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
        private LoadMeter loadMeter;
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
                loadMeter = CreateLoadMeter(this, picLoadInfo, dspComponent);
                recordingState = new RecordingState(this);
                CheckIntervalLength();

                try
                {
                    await dspComponent.DspContext.Completion;
                }
                catch (Exception exception)
                {
                    using (var box = new ErrorBox("アプリは中断されました。", exception))
                    {
                        box.ShowDialog();
                    }
                    Close();
                }
            }
            catch (Exception exception)
            {
                using (var box = new ErrorBox("アプリを起動できませんでした。", exception))
                {
                    box.ShowDialog();
                }
                Close();
            }
        }

        private static LoadMeter CreateLoadMeter(Form form, PictureBox pictureBox, DspComponent dspComponent)
        {
            var names = new string[]
            {
                "DSP",
                "描画",
                "録音"
            };

            var intervalTime = (double)dspComponent.DspDriver.IntervalLength / dspComponent.DspDriver.SampleRate;
            var bufferLength = Math.Min(dspComponent.AsioDspSetting.BufferLength, dspComponent.DspDriver.SampleRate);
            var dspContext = dspComponent.DspContext;
            var waveformMonitor = dspComponent.WaveformMonitor;
            var waveRecorder = dspComponent.WaveRecorder;

            var sources = new Func<double>[]
            {
                () => dspComponent.Watchdog.DspTime / intervalTime,
                () => (double)(dspContext.ProcessedSampleCount - waveformMonitor.ProcessedSampleCount) / bufferLength,
                () => (double)(dspContext.ProcessedSampleCount - waveRecorder.ProcessedSampleCount) / bufferLength
            };

            return new LoadMeter(form, pictureBox, names, sources);
        }

        private void CheckIntervalLength()
        {
            var intervalTime = (double)dspComponent.DspDriver.IntervalLength / dspComponent.DspDriver.SampleRate;
            if (intervalTime < 0.01)
            {
                var message =
                    "オーディオデバイスのバッファ長が 10 ms 未満に設定されています。" + Environment.NewLine +
                    "アプリの動作を安定させるため、バッファ長を 10 ms 以上に設定することを推奨します。";
                MessageBox.Show(message, "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    dspComponent?.DspContext.SendKeyDownEvent(e.KeyCode.ToString());
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
            loadMeter?.Dispose();
            dspComponent?.Dispose();
        }



        private class DspComponent : IDisposable
        {
            private AsioDspSetting asioDspSetting;
            private IDspDriver dspDriver;
            private InputGain inputGain;
            private Bypass bypass;
            private OutputGain outputGain;
            private WaveformMonitor waveformMonitor;
            private WaveRecorder waveRecorder;
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
                    //asioDspSetting.UseLongInterval = false;

                    dspDriver = new AsioDspDriver(asioDspSetting);
                    //dspDriver = new FileDspDriver("test.wav",  2345);
                    //dspDriver = new FileDspDriver("test.wav", "out.wav", 2, 2345);

                    inputGain = new InputGain(dspDriver, setting.InputGains);
                    dspDriver.AddDsp(inputGain);

                    bypass = new Bypass(dspDriver);
                    dspDriver.AddDsp(bypass);

                    outputGain = new OutputGain(dspDriver, setting.OutputGains);
                    dspDriver.AddDsp(outputGain);

                    waveformMonitor = new WaveformMonitor(dspDriver, form.picWaveformMonitor, setting.UpdateInterval, setting.DrawCycle, false);
                    dspDriver.AddDsp(waveformMonitor);

                    waveRecorder = new WaveRecorder(dspDriver);
                    dspDriver.AddDsp(waveRecorder);

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
                if (waveRecorder != null)
                {
                    waveRecorder.Dispose();
                    waveRecorder = null;
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

            public WaveRecorder WaveRecorder
            {
                get
                {
                    return waveRecorder;
                }
            }

            public Watchdog Watchdog
            {
                get
                {
                    return watchdog;
                }
            }

            public CommandPicker CommandPicker
            {
                get
                {
                    return commandPicker;
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
                if (form.dspComponent.DspDriver.OutputChannelCount > 0)
                {
                    form.dspComponent.WaveformMonitor.SetChannelFocus(channelFocus);
                }

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
                    form.loadMeter.SetRecordingState(true);
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
                    form.loadMeter.SetRecordingState(false);
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

            public void RecordingComplete()
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
                if (form.dspComponent.DspDriver.OutputChannelCount > 0)
                {
                    channelFocus = Math.Max(channelFocus - 1, 0);
                    for (var ch = 0; ch < form.dspComponent.DspDriver.OutputChannelCount; ch++)
                    {
                        form.dspComponent.Bypass.SetConnection(channelFocus, ch);
                    }
                    form.dspComponent.WaveformMonitor.SetChannelFocus(channelFocus);
                    form.picWaveformMonitor.Refresh();
                }
            }

            public void DownChannelFocus()
            {
                if (form.dspComponent.DspDriver.OutputChannelCount > 0)
                {
                    channelFocus = Math.Min(channelFocus + 1, form.dspComponent.DspDriver.InputChannelCount - 1);
                    for (var ch = 0; ch < form.dspComponent.DspDriver.OutputChannelCount; ch++)
                    {
                        form.dspComponent.Bypass.SetConnection(channelFocus, ch);
                    }
                    form.dspComponent.WaveformMonitor.SetChannelFocus(channelFocus);
                    form.picWaveformMonitor.Refresh();
                }
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
                var recordingCompleteCommand = command as RecordingCompleteCommand;
                if (recordingCompleteCommand != null)
                {
                    Process(context, recordingCompleteCommand);
                }
            }

            public void Process(IDspContext context, RecordingCompleteCommand command)
            {
                form.Invoke((MethodInvoker)(() => form.recordingState.RecordingComplete()));
            }
        }
    }
}
