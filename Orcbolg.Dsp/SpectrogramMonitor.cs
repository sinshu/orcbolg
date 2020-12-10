using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Numerics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Forms;

namespace Orcbolg.Dsp
{
    public sealed class SpectrogramMonitor : INonrealtimeDsp, IDisposable
    {
        private readonly PictureBox pictureBox;
        private readonly int frameLength;
        private readonly int updateInterval;
        private readonly int drawCycle;
        private readonly bool showOutput;

        private readonly int channelCount;
        private readonly int sampleRate;
        private readonly string[] channelNames;

        private Font font;
        private Brush channelNameColor;

        private int focusedChannel;
        private Brush focusedChannelNameColor;

        private bool recording;
        private int recordingNumber;
        private Brush recordingColor;

        private List<Tuple<string, Color>> messages;

        private double[] window;
        private double[] amplitude;
        private StftAnalysis stftAnalysis;

        private Brush backgroundColor;
        private Brush gridColor;

        private Bitmap ui_buffer;
        private Graphics ui_g;
        private int ui_x;
        private int ui_sampleCount;
        private int ui_drawCycleCount;

        private long processedSampleCount;

        private Stopwatch timer;
        private object mutex;
        private double maxTime;
        private double weight;
        private double dspTime;
        private double cpuLoad;

        public SpectrogramMonitor(IDspDriver driver, PictureBox pictureBox, int frameLength, int updateInterval, int drawCycle, bool showOutput)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (pictureBox == null) throw new ArgumentNullException(nameof(pictureBox));

            if ((double)updateInterval / driver.SampleRate < 0.001)
            {
                throw new ArgumentOutOfRangeException("The update interval must be greater than or equal to 1 ms.");
            }
            if (drawCycle <= 0)
            {
                throw new ArgumentOutOfRangeException("The draw cycle must be greater than or equal to one.");
            }

            this.pictureBox = pictureBox;
            this.frameLength = frameLength;
            this.updateInterval = updateInterval;
            this.drawCycle = drawCycle;
            this.showOutput = showOutput;

            try
            {
                channelCount = showOutput ? driver.InputChannelCount + driver.OutputChannelCount : driver.InputChannelCount;
                sampleRate = driver.SampleRate;
                channelNames = new string[channelCount];
                {
                    var i = 0;
                    for (var ch = 0; ch < driver.InputChannelCount; ch++)
                    {
                        channelNames[i] = driver.GetInputChannelName(ch) + Environment.NewLine;
                        i++;
                    }
                    if (showOutput)
                    {
                        for (var ch = 0; ch < driver.OutputChannelCount; ch++)
                        {
                            channelNames[i] = driver.GetOutputChannelName(ch) + Environment.NewLine;
                            i++;
                        }
                    }
                }

                font = new Font(FontFamily.GenericMonospace, 9);
                channelNameColor = new SolidBrush(Color.FromArgb(250, 250, 250));

                focusedChannel = -1;
                focusedChannelNameColor = new SolidBrush(Color.FromArgb(244, 67, 54));

                recording = false;
                recordingNumber = 0;
                recordingColor = new SolidBrush(Color.FromArgb(64, 244, 67, 54));

                messages = new List<Tuple<string, Color>>();

                window = WindowFunc.CreateHann(frameLength);
                amplitude = new double[frameLength / 2 + 1];
                ResetSignalStats();

                backgroundColor = new SolidBrush(Color.FromArgb(33, 33, 33));
                gridColor = new SolidBrush(Color.FromArgb(32, 250, 250, 250));

                Reset();

                processedSampleCount = 0;

                timer = new Stopwatch();
                mutex = new object();
                maxTime = (double)driver.IntervalLength / driver.SampleRate;
                weight = Math.Pow(10, -3 / ((double)driver.SampleRate / driver.IntervalLength));
                dspTime = 0;
                cpuLoad = 0;

                pictureBox.Paint += PictureBox_Paint;
            }
            catch (Exception e)
            {
                Dispose();
                ExceptionDispatchInfo.Capture(e).Throw();
            }
        }

        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            {
                var srcRect = new Rectangle(ui_x, 0, ui_buffer.Width - ui_x, ui_buffer.Height);
                e.Graphics.DrawImage(ui_buffer, 0, 0, srcRect, GraphicsUnit.Pixel);
            }
            {
                var srcRect = new Rectangle(0, 0, ui_x, ui_buffer.Height);
                e.Graphics.DrawImage(ui_buffer, ui_buffer.Width - ui_x, 0, srcRect, GraphicsUnit.Pixel);
            }
            for (var ch = 0; ch < channelCount; ch++)
            {
                var top = (int)Math.Round((double)ui_buffer.Height * ch / channelCount);
                var brush = ch == focusedChannel ? focusedChannelNameColor : channelNameColor;
                e.Graphics.DrawString(channelNames[ch], font, brush, 2, top + 2);
            }
        }

        private void ResetSignalStats()
        {
            stftAnalysis = new StftAnalysis(channelCount, window, frameLength, Test);
        }

        private void Test(IDspContext context, long position, Complex[][] dft)
        {
            pictureBox.Invoke((MethodInvoker)(() => UpdateBuffer(context, dft)));
        }

        private unsafe void UpdateBuffer(IDspContext context, Complex[][] dft)
        {
            var lockArea = new Rectangle(ui_x, 0, 1, ui_buffer.Height);
            var data = ui_buffer.LockBits(lockArea, ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
            for (var ch = 0; ch < channelCount; ch++)
            {
                DftAnalysis.GetNormalizedAmplitude(dft[ch], amplitude);

                var top = (int)Math.Round((double)ui_buffer.Height * ch / channelCount);
                var bottom = (int)Math.Round((double)ui_buffer.Height * (ch + 1) / channelCount);
                var length = bottom - top;
                var p = (uint*)data.Scan0;
                var stride = data.Stride / 4;
                p += stride * top;

                var dftFrac = (double)(frameLength / 2) + 0.5;
                var dftStep = (double)(frameLength / 2) / length;

                for (var y = 0; y < length; y++)
                {
                    var max = 0.0;
                    var dftEnd = (int)dftFrac;
                    dftFrac -= dftStep;
                    var dftStart = (int)(dftFrac);
                    for (var w = dftStart; w < dftEnd; w++)
                    {
                        if (amplitude[w] > max)
                        {
                            max = amplitude[w];
                        }
                    }

                    var val = (int)(3 * (20 * Math.Log10(max) + 120));
                    if (val < 0) val = 0;
                    if (val > 255) val = 255;

                    *p = colors[val];
                    p += stride;
                }
            }
            ui_buffer.UnlockBits(data);

            for (var ch = 0; ch < channelCount; ch++)
            {
                var top = (int)Math.Round((double)ui_buffer.Height * ch / channelCount);
                var bottom = (int)Math.Round((double)ui_buffer.Height * (ch + 1) / channelCount);
                ui_g.FillRectangle(gridColor, ui_x, bottom - 1, 1, 1);
            }

            ui_sampleCount += updateInterval;
            if (ui_sampleCount >= sampleRate)
            {
                ui_g.FillRectangle(gridColor, ui_x, 0, 1, ui_buffer.Height);
                ui_sampleCount -= sampleRate;
            }

            if (recording)
            {
                ui_g.FillRectangle(recordingColor, ui_x, 0, 1, ui_buffer.Height);
            }

            if (messages.Count > 0)
            {
                foreach (var tuple in messages)
                {
                    var size = ui_g.MeasureString(tuple.Item1, font);
                    using (var brush = new SolidBrush(tuple.Item2))
                    {
                        ui_g.FillRectangle(brush, ui_x, 0, 1, ui_buffer.Height);
                        ui_g.DrawString(tuple.Item1, font, brush, ui_x - size.Width, 2);
                        ui_g.DrawString(tuple.Item1, font, brush, ui_x - size.Width + ui_buffer.Width, 2);
                    }
                }
                messages.Clear();
            }

            ui_x++;
            if (ui_x == ui_buffer.Width)
            {
                ui_x = 0;
            }

            ui_drawCycleCount++;
            if (ui_drawCycleCount == drawCycle)
            {
                if (context.ProcessedSampleCount - processedSampleCount < sampleRate / 2)
                {
                    pictureBox.Refresh();
                }
                ui_drawCycleCount = 0;
            }
        }

        public void Reset()
        {
            if (ui_g != null)
            {
                ui_g.Dispose();
            }
            if (ui_buffer != null)
            {
                ui_buffer.Dispose();
            }
            var width = Math.Max(pictureBox.Width, 1);
            var height = Math.Max(pictureBox.Height, 1);
            ui_buffer = new Bitmap(width, height, PixelFormat.Format32bppRgb);
            ui_g = Graphics.FromImage(ui_buffer);
            ui_g.Clear(SystemColors.ControlDarkDark);
            ui_x = 0;
            ui_sampleCount = 0;
            ui_drawCycleCount = 0;
            pictureBox.Refresh();
        }

        public void Resize()
        {
            var width = Math.Max(pictureBox.Width, 1);
            var height = Math.Max(pictureBox.Height, 1);
            if (ui_buffer.Width != width || ui_buffer.Height != height)
            {
                Reset();
            }
        }

        public void Process(IDspContext context, IDspCommand command)
        {
            var intervalCommand = command as IntervalCommand;
            if (intervalCommand != null)
            {
                timer.Start();

                Process(context, intervalCommand);

                timer.Stop();
                var epalsed = timer.Elapsed.TotalSeconds;
                dspTime = weight * dspTime + (1 - weight) * epalsed;
                var newCpuLoad = Math.Min(dspTime / maxTime, 1.0);
                lock (mutex)
                {
                    cpuLoad = newCpuLoad;
                }
                timer.Reset();
            }

            var recordingStartCommand = command as RecordingStartCommand;
            if (recordingStartCommand != null)
            {
                Process(context, recordingStartCommand);
            }

            var recordingCompleteCommand = command as RecordingCompleteCommand;
            if (recordingCompleteCommand != null)
            {
                Process(context, recordingCompleteCommand);
            }

            var recordingAbortCommand = command as RecordingAbortCommand;
            if (recordingAbortCommand != null)
            {
                Process(context, recordingAbortCommand);
            }

            var keyDownCommand = command as KeyDownCommand;
            if (keyDownCommand != null)
            {
                Process(context, keyDownCommand);
            }

            var jumpingWarningCommand = command as JumpingWarningCommand;
            if (jumpingWarningCommand != null)
            {
                Process(context, jumpingWarningCommand);
            }
        }

        private void Process(IDspContext context, IntervalCommand command)
        {
            if (showOutput)
            {
                var interval = command.InputInterval.Concat(command.OutputInterval).ToArray();
                stftAnalysis.Process(context, interval, command.Length);
            }
            else
            {
                stftAnalysis.Process(context, command.InputInterval, command.Length);
            }

            Interlocked.Add(ref processedSampleCount, command.Length);
        }

        private void Process(IDspContext context, RecordingStartCommand command)
        {
            recording = true;
            recordingNumber = command.Number;
            messages.Add(Tuple.Create("REC" + Environment.NewLine + "[" + command.Number + "]", Color.FromArgb(240, 244, 67, 54)));
        }

        private void Process(IDspContext context, RecordingCompleteCommand command)
        {
            recording = false;
            messages.Add(Tuple.Create("STOP" + Environment.NewLine + "[" + recordingNumber + "]", Color.FromArgb(240, 244, 67, 54)));
        }

        private void Process(IDspContext context, RecordingAbortCommand command)
        {
            recording = false;
            messages.Add(Tuple.Create("STOP" + Environment.NewLine + "[" + recordingNumber + "]", Color.FromArgb(240, 244, 67, 54)));
        }

        private void Process(IDspContext context, KeyDownCommand command)
        {
            messages.Add(Tuple.Create("Key" + Environment.NewLine + "[" + command.Value + "]", Color.FromArgb(240, 0, 188, 212)));
        }

        private void Process(IDspContext context, JumpingWarningCommand command)
        {
            messages.Add(Tuple.Create("JUMPING" + Environment.NewLine, Color.FromArgb(240, 255, 152, 0)));
        }

        public void SetChannelFocus(int channel)
        {
            focusedChannel = channel;
        }

        public void Dispose()
        {
            pictureBox.Paint -= PictureBox_Paint;

            if (ui_g != null)
            {
                ui_g.Dispose();
                ui_g = null;
            }
            if (ui_buffer != null)
            {
                ui_buffer.Dispose();
                ui_buffer = null;
            }

            if (backgroundColor != null)
            {
                backgroundColor.Dispose();
                backgroundColor = null;
            }
            if (gridColor != null)
            {
                gridColor.Dispose();
                gridColor = null;
            }

            if (recordingColor != null)
            {
                recordingColor.Dispose();
                recordingColor = null;
            }

            if (focusedChannelNameColor != null)
            {
                focusedChannelNameColor.Dispose();
                focusedChannelNameColor = null;
            }

            if (font != null)
            {
                font.Dispose();
                font = null;
            }
            if (channelNameColor != null)
            {
                channelNameColor.Dispose();
                channelNameColor = null;
            }
        }

        public long ProcessedSampleCount
        {
            get
            {
                return Interlocked.Read(ref processedSampleCount);
            }
        }

        public double CpuLoad
        {
            get
            {
                lock (mutex)
                {
                    return cpuLoad;
                }
            }
        }



        private static readonly uint[] colors = new uint[]
        {
                        0xFF212124u,
            0xFF212125u,
            0xFF212126u,
            0xFF222127u,
            0xFF222229u,
            0xFF22222Bu,
            0xFF23222Du,
            0xFF23232Fu,
            0xFF242331u,
            0xFF242332u,
            0xFF252434u,
            0xFF262436u,
            0xFF262538u,
            0xFF27253Au,
            0xFF28263Cu,
            0xFF29263Eu,
            0xFF2A2740u,
            0xFF2B2742u,
            0xFF2C2844u,
            0xFF2D2846u,
            0xFF2E2948u,
            0xFF2F294Au,
            0xFF30294Cu,
            0xFF322A4Eu,
            0xFF332A51u,
            0xFF342A53u,
            0xFF352B55u,
            0xFF372B57u,
            0xFF382B59u,
            0xFF392B5Bu,
            0xFF3B2B5Du,
            0xFF3C2B5Fu,
            0xFF3D2B61u,
            0xFF3F2B63u,
            0xFF402B65u,
            0xFF422B67u,
            0xFF432A69u,
            0xFF452A6Bu,
            0xFF462A6Du,
            0xFF482A6Eu,
            0xFF492970u,
            0xFF4B2971u,
            0xFF4C2972u,
            0xFF4E2974u,
            0xFF502975u,
            0xFF512976u,
            0xFF532977u,
            0xFF542978u,
            0xFF562979u,
            0xFF57297Au,
            0xFF59297Au,
            0xFF5A297Bu,
            0xFF5C297Cu,
            0xFF5D2A7Cu,
            0xFF5E2A7Du,
            0xFF602A7Du,
            0xFF612B7Eu,
            0xFF632B7Eu,
            0xFF642C7Fu,
            0xFF662C7Fu,
            0xFF672C7Fu,
            0xFF682D80u,
            0xFF6A2D80u,
            0xFF6B2E80u,
            0xFF6D2E80u,
            0xFF6E2F80u,
            0xFF6F2F81u,
            0xFF713081u,
            0xFF723081u,
            0xFF743181u,
            0xFF753281u,
            0xFF763281u,
            0xFF783381u,
            0xFF793381u,
            0xFF7B3481u,
            0xFF7C3481u,
            0xFF7D3581u,
            0xFF7F3581u,
            0xFF803681u,
            0xFF823681u,
            0xFF833781u,
            0xFF843781u,
            0xFF863881u,
            0xFF873880u,
            0xFF883980u,
            0xFF8A3980u,
            0xFF8B3A80u,
            0xFF8D3A80u,
            0xFF8E3B7Fu,
            0xFF8F3B7Fu,
            0xFF913C7Fu,
            0xFF923C7Fu,
            0xFF943D7Eu,
            0xFF953D7Eu,
            0xFF963E7Eu,
            0xFF983E7Du,
            0xFF993F7Du,
            0xFF9B3F7Du,
            0xFF9C407Cu,
            0xFF9D407Cu,
            0xFF9F417Cu,
            0xFFA0417Bu,
            0xFFA1427Bu,
            0xFFA3427Au,
            0xFFA4437Au,
            0xFFA64379u,
            0xFFA74479u,
            0xFFA84478u,
            0xFFAA4578u,
            0xFFAB4577u,
            0xFFAD4677u,
            0xFFAE4676u,
            0xFFAF4775u,
            0xFFB14775u,
            0xFFB24874u,
            0xFFB34874u,
            0xFFB54973u,
            0xFFB64A72u,
            0xFFB74A72u,
            0xFFB94B71u,
            0xFFBA4B70u,
            0xFFBB4C70u,
            0xFFBD4D6Fu,
            0xFFBE4D6Eu,
            0xFFBF4E6Du,
            0xFFC14F6Du,
            0xFFC24F6Cu,
            0xFFC3506Bu,
            0xFFC5516Au,
            0xFFC65169u,
            0xFFC75268u,
            0xFFC85368u,
            0xFFCA5467u,
            0xFFCB5466u,
            0xFFCC5565u,
            0xFFCD5664u,
            0xFFCF5763u,
            0xFFD05862u,
            0xFFD15861u,
            0xFFD25961u,
            0xFFD35A60u,
            0xFFD45B5Fu,
            0xFFD65C5Eu,
            0xFFD75D5Du,
            0xFFD85E5Cu,
            0xFFD95F5Bu,
            0xFFDA605Au,
            0xFFDB6159u,
            0xFFDC6258u,
            0xFFDD6357u,
            0xFFDE6456u,
            0xFFDF6555u,
            0xFFE06654u,
            0xFFE16753u,
            0xFFE26852u,
            0xFFE36951u,
            0xFFE46B50u,
            0xFFE56C4Fu,
            0xFFE66D4Eu,
            0xFFE76E4Du,
            0xFFE86F4Cu,
            0xFFE9714Bu,
            0xFFEA724Au,
            0xFFEA7348u,
            0xFFEB7447u,
            0xFFEC7646u,
            0xFFED7745u,
            0xFFEE7844u,
            0xFFEE7A43u,
            0xFFEF7B42u,
            0xFFF07C41u,
            0xFFF17E40u,
            0xFFF17F3Fu,
            0xFFF2803Eu,
            0xFFF2823Cu,
            0xFFF3833Bu,
            0xFFF4853Au,
            0xFFF48639u,
            0xFFF58838u,
            0xFFF58937u,
            0xFFF68B36u,
            0xFFF68C34u,
            0xFFF78D33u,
            0xFFF78F32u,
            0xFFF89131u,
            0xFFF89230u,
            0xFFF9942Fu,
            0xFFF9952Eu,
            0xFFF9972Cu,
            0xFFFA982Bu,
            0xFFFA9A2Au,
            0xFFFA9B29u,
            0xFFFB9D28u,
            0xFFFB9F28u,
            0xFFFBA027u,
            0xFFFBA226u,
            0xFFFCA326u,
            0xFFFCA526u,
            0xFFFCA726u,
            0xFFFCA826u,
            0xFFFCAA26u,
            0xFFFCAB27u,
            0xFFFDAD27u,
            0xFFFDAF28u,
            0xFFFDB029u,
            0xFFFDB22Bu,
            0xFFFDB42Cu,
            0xFFFDB52Eu,
            0xFFFDB72Fu,
            0xFFFDB931u,
            0xFFFDBA32u,
            0xFFFDBC34u,
            0xFFFDBE36u,
            0xFFFCC038u,
            0xFFFCC13Au,
            0xFFFCC33Bu,
            0xFFFCC53Du,
            0xFFFCC63Fu,
            0xFFFCC841u,
            0xFFFBCA43u,
            0xFFFBCC46u,
            0xFFFBCD48u,
            0xFFFACF4Au,
            0xFFFAD14Cu,
            0xFFFAD24Fu,
            0xFFF9D451u,
            0xFFF9D653u,
            0xFFF9D856u,
            0xFFF8D958u,
            0xFFF8DB5Bu,
            0xFFF8DD5Du,
            0xFFF7DE60u,
            0xFFF7E063u,
            0xFFF6E266u,
            0xFFF6E369u,
            0xFFF5E56Cu,
            0xFFF5E76Fu,
            0xFFF5E872u,
            0xFFF4EA75u,
            0xFFF4EC79u,
            0xFFF4ED7Cu,
            0xFFF4EF80u,
            0xFFF4F083u,
            0xFFF4F187u,
            0xFFF4F38Au,
            0xFFF4F48Eu,
            0xFFF4F592u,
            0xFFF5F795u,
            0xFFF5F899u,
            0xFFF6F99Du,
            0xFFF7FAA0u,
            0xFFF8FBA3u,
            0xFFF9FCA7u,
            0xFFFAFDAAu,
            0xFFFCFEADu,
            0xFFFDFFB0u
        };
    }
}
