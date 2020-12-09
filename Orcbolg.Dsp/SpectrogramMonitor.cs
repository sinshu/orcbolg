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

                    var val = (int)(2 * (20 * Math.Log10(max) + 120));
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
            0xFF000003u,
            0xFF000004u,
            0xFF000006u,
            0xFF010007u,
            0xFF010109u,
            0xFF01010Bu,
            0xFF02010Eu,
            0xFF020210u,
            0xFF030212u,
            0xFF040314u,
            0xFF040316u,
            0xFF050418u,
            0xFF06041Bu,
            0xFF07051Du,
            0xFF08061Fu,
            0xFF090621u,
            0xFF0A0724u,
            0xFF0C0726u,
            0xFF0D0828u,
            0xFF0E082Bu,
            0xFF0F092Du,
            0xFF10092Fu,
            0xFF120A32u,
            0xFF130A34u,
            0xFF140B37u,
            0xFF160B39u,
            0xFF170B3Bu,
            0xFF190B3Eu,
            0xFF1A0C40u,
            0xFF1C0C43u,
            0xFF1D0C45u,
            0xFF1F0C48u,
            0xFF210C4Au,
            0xFF220B4Cu,
            0xFF240B4Eu,
            0xFF260B51u,
            0xFF270B53u,
            0xFF290B55u,
            0xFF2B0A57u,
            0xFF2D0A59u,
            0xFF2E0A5Au,
            0xFF300A5Cu,
            0xFF32095Eu,
            0xFF34095Fu,
            0xFF360960u,
            0xFF370962u,
            0xFF390963u,
            0xFF3B0964u,
            0xFF3C0965u,
            0xFF3E0966u,
            0xFF400967u,
            0xFF420968u,
            0xFF430A68u,
            0xFF450A69u,
            0xFF470A6Au,
            0xFF480B6Au,
            0xFF4A0B6Bu,
            0xFF4C0C6Bu,
            0xFF4D0C6Cu,
            0xFF4F0D6Cu,
            0xFF500D6Cu,
            0xFF520E6Du,
            0xFF540E6Du,
            0xFF550F6Du,
            0xFF570F6Du,
            0xFF59106Eu,
            0xFF5A116Eu,
            0xFF5C116Eu,
            0xFF5D126Eu,
            0xFF5F126Eu,
            0xFF61136Eu,
            0xFF62146Eu,
            0xFF64146Eu,
            0xFF65156Eu,
            0xFF67156Eu,
            0xFF68166Eu,
            0xFF6A176Eu,
            0xFF6C176Eu,
            0xFF6D186Eu,
            0xFF6F186Eu,
            0xFF70196Eu,
            0xFF721A6Eu,
            0xFF741A6Eu,
            0xFF751B6Eu,
            0xFF771B6Du,
            0xFF781C6Du,
            0xFF7A1C6Du,
            0xFF7C1D6Du,
            0xFF7D1D6Cu,
            0xFF7F1E6Cu,
            0xFF801F6Cu,
            0xFF821F6Cu,
            0xFF84206Bu,
            0xFF85206Bu,
            0xFF87216Bu,
            0xFF88216Au,
            0xFF8A226Au,
            0xFF8C2369u,
            0xFF8D2369u,
            0xFF8F2468u,
            0xFF902468u,
            0xFF922568u,
            0xFF942567u,
            0xFF952667u,
            0xFF972766u,
            0xFF982765u,
            0xFF9A2865u,
            0xFF9B2864u,
            0xFF9D2964u,
            0xFF9F2A63u,
            0xFFA02A62u,
            0xFFA22B62u,
            0xFFA32B61u,
            0xFFA52C60u,
            0xFFA72D60u,
            0xFFA82D5Fu,
            0xFFAA2E5Eu,
            0xFFAB2F5Du,
            0xFFAD2F5Du,
            0xFFAE305Cu,
            0xFFB0315Bu,
            0xFFB1315Au,
            0xFFB33259u,
            0xFFB43359u,
            0xFFB63458u,
            0xFFB73457u,
            0xFFB93556u,
            0xFFBA3655u,
            0xFFBC3754u,
            0xFFBD3853u,
            0xFFBF3852u,
            0xFFC03951u,
            0xFFC23A50u,
            0xFFC33B4Fu,
            0xFFC53C4Eu,
            0xFFC63D4Du,
            0xFFC73E4Cu,
            0xFFC93F4Bu,
            0xFFCA404Au,
            0xFFCB4149u,
            0xFFCD4248u,
            0xFFCE4347u,
            0xFFCF4446u,
            0xFFD14545u,
            0xFFD24644u,
            0xFFD34743u,
            0xFFD54841u,
            0xFFD64940u,
            0xFFD74A3Fu,
            0xFFD84C3Eu,
            0xFFD94D3Du,
            0xFFDB4E3Cu,
            0xFFDC4F3Bu,
            0xFFDD5139u,
            0xFFDE5238u,
            0xFFDF5337u,
            0xFFE05536u,
            0xFFE15635u,
            0xFFE25733u,
            0xFFE35932u,
            0xFFE45A31u,
            0xFFE55B30u,
            0xFFE65D2Fu,
            0xFFE75E2Du,
            0xFFE8602Cu,
            0xFFE9612Bu,
            0xFFEA632Au,
            0xFFEB6428u,
            0xFFEC6627u,
            0xFFED6726u,
            0xFFED6925u,
            0xFFEE6A23u,
            0xFFEF6C22u,
            0xFFF06E21u,
            0xFFF16F20u,
            0xFFF1711Eu,
            0xFFF2721Du,
            0xFFF3741Cu,
            0xFFF3761Au,
            0xFFF47719u,
            0xFFF47918u,
            0xFFF57B16u,
            0xFFF67D15u,
            0xFFF67E14u,
            0xFFF78012u,
            0xFFF78211u,
            0xFFF88410u,
            0xFFF8850Eu,
            0xFFF8870Du,
            0xFFF9890Cu,
            0xFFF98B0Bu,
            0xFFFA8D09u,
            0xFFFA8E08u,
            0xFFFA9008u,
            0xFFFB9207u,
            0xFFFB9406u,
            0xFFFB9606u,
            0xFFFB9806u,
            0xFFFC9906u,
            0xFFFC9B06u,
            0xFFFC9D06u,
            0xFFFC9F07u,
            0xFFFCA107u,
            0xFFFCA308u,
            0xFFFCA50Au,
            0xFFFCA70Bu,
            0xFFFCA90Du,
            0xFFFCAA0Eu,
            0xFFFCAC10u,
            0xFFFCAE12u,
            0xFFFCB014u,
            0xFFFCB216u,
            0xFFFCB418u,
            0xFFFCB61Au,
            0xFFFCB81Cu,
            0xFFFCBA1Eu,
            0xFFFBBC21u,
            0xFFFBBE23u,
            0xFFFBC025u,
            0xFFFBC228u,
            0xFFFAC42Au,
            0xFFFAC62Du,
            0xFFFAC82Fu,
            0xFFF9CA32u,
            0xFFF9CC34u,
            0xFFF9CE37u,
            0xFFF8D03Au,
            0xFFF8D23Du,
            0xFFF7D43Fu,
            0xFFF7D642u,
            0xFFF6D845u,
            0xFFF6D949u,
            0xFFF5DB4Cu,
            0xFFF5DD4Fu,
            0xFFF4DF52u,
            0xFFF4E156u,
            0xFFF4E359u,
            0xFFF3E55Du,
            0xFFF3E761u,
            0xFFF2E965u,
            0xFFF2EA69u,
            0xFFF2EC6Du,
            0xFFF2EE71u,
            0xFFF2EF75u,
            0xFFF2F179u,
            0xFFF2F37Du,
            0xFFF3F482u,
            0xFFF3F586u,
            0xFFF4F78Au,
            0xFFF5F88Eu,
            0xFFF6F992u,
            0xFFF7FB96u,
            0xFFF8FC9Au,
            0xFFF9FD9Du,
            0xFFFBFEA1u,
            0xFFFDFFA5u
        };
    }
}
