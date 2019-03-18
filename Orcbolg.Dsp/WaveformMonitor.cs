using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace Orcbolg.Dsp
{
    public sealed class WaveformMonitor : INonrealtimeDsp, IDisposable
    {
        private readonly PictureBox pictureBox;
        private readonly int drawInterval;
        private readonly bool drawOutput;

        private readonly int channelCount;
        private readonly int sampleRate;
        private readonly string[] channelNames;

        private readonly List<Tuple<string, Color>> tuples;

        private double[] bg_min;
        private double[] bg_max;
        private double[] bg_sum;
        private double[] bg_squaredSum;
        private int bg_sampleCount;

        private Brush background;
        private Brush grid;
        private Brush clip;
        private Brush[] waveform1;
        private Brush[] waveform2;

        private Bitmap ui_buffer;
        private Graphics ui_g;
        private int ui_x;
        private int ui_sampleCount;

        private Brush recordingColor;
        private bool recording;

        private Font font;
        private List<Tuple<string, Color>> messages;

        public WaveformMonitor(IDspDriver driver, PictureBox pictureBox, int drawInterval, bool drawOutput)
        {
            try
            {
                this.pictureBox = pictureBox;
                this.drawInterval = drawInterval;
                this.drawOutput = drawOutput;

                channelCount = drawOutput ? driver.InputChannelCount + driver.OutputChannelCount : driver.InputChannelCount;
                sampleRate = driver.SampleRate;
                channelNames = new string[channelCount];
                {
                    var i = 0;
                    for (var ch = 0; ch < driver.InputChannelCount; ch++)
                    {
                        channelNames[i] = driver.GetInputChannelName(ch) + Environment.NewLine;
                        i++;
                    }
                    if (drawOutput)
                    {
                        for (var ch = 0; ch < driver.OutputChannelCount; ch++)
                        {
                            channelNames[i] = driver.GetOutputChannelName(ch) + Environment.NewLine;
                            i++;
                        }
                    }
                }

                tuples = new List<Tuple<string, Color>>();

                bg_min = new double[channelCount];
                bg_max = new double[channelCount];
                bg_sum = new double[channelCount];
                bg_squaredSum = new double[channelCount];
                ResetStats();

                background = new SolidBrush(Color.FromArgb(33, 33, 33));
                grid = new SolidBrush(Color.FromArgb(32, 250, 250, 250));
                clip = new SolidBrush(Color.FromArgb(240, 244, 67, 54));
                waveform1 = new Brush[channelCount];
                waveform2 = new Brush[channelCount];
                {
                    var i = 0;
                    for (var ch = 0; ch < driver.InputChannelCount; ch++)
                    {
                        waveform1[i] = new SolidBrush(Color.FromArgb(0, 230, 118));
                        waveform2[i] = new SolidBrush(Color.FromArgb(192, 0, 230, 118));
                        i++;
                    }
                    if (drawOutput)
                    {
                        for (var ch = 0; ch < driver.OutputChannelCount; ch++)
                        {
                            waveform1[i] = new SolidBrush(Color.FromArgb(0, 176, 255));
                            waveform2[i] = new SolidBrush(Color.FromArgb(192, 0, 176, 255));
                            i++;
                        }
                    }
                }

                recordingColor = new SolidBrush(Color.FromArgb(64, 244, 67, 54));
                recording = false;

                font = new Font(FontFamily.GenericMonospace, 9);
                messages = new List<Tuple<string, Color>>();

                Reset();

                pictureBox.Paint += PictureBox_Paint;
            }
            catch
            {
                Dispose();
                throw;
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
                e.Graphics.DrawString(channelNames[ch], font, Brushes.White, 2, top + 2);
            }
        }

        private void ResetStats()
        {
            for (var ch = 0; ch < channelCount; ch++)
            {
                bg_min[ch] = double.MaxValue;
                bg_max[ch] = double.MinValue;
                Array.Clear(bg_sum, 0, bg_sum.Length);
                Array.Clear(bg_squaredSum, 0, bg_squaredSum.Length);
            }
            bg_sampleCount = 0;
        }

        public void Reset()
        {
            if (ui_buffer != null)
            {
                ui_g.Dispose();
                ui_buffer.Dispose();
            }
            var width = Math.Max(pictureBox.Width, 1);
            var height = Math.Max(pictureBox.Height, 1);
            ui_buffer = new Bitmap(width, height, PixelFormat.Format32bppRgb);
            ui_g = Graphics.FromImage(ui_buffer);
            ui_g.Clear(SystemColors.ControlDarkDark);
            ui_x = 0;
            ui_sampleCount = 0;
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
                Process(context, intervalCommand);
            }

            var recordingStartCommand = command as RecordingStartCommand;
            if (recordingStartCommand != null)
            {
                Process(context, recordingStartCommand);
            }

            var recordingStopCommand = command as RecordingStopCommand;
            if (recordingStopCommand != null)
            {
                Process(context, recordingStopCommand);
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
            for (var t = 0; t < command.Length; t++)
            {
                var i = 0;
                for (var ch = 0; ch < command.InputInterval.Length; ch++)
                {
                    var sample = command.InputInterval[ch][t];
                    if (sample < bg_min[i])
                    {
                        bg_min[i] = sample;
                    }
                    if (sample > bg_max[i])
                    {
                        bg_max[i] = sample;
                    }
                    bg_sum[i] += sample;
                    bg_squaredSum[i] += sample * sample;
                    i++;
                }
                if (drawOutput)
                {
                    for (var ch = 0; ch < command.OutputInterval.Length; ch++)
                    {
                        var sample = command.InputInterval[ch][t];
                        if (sample < bg_min[i])
                        {
                            bg_min[i] = sample;
                        }
                        if (sample > bg_max[i])
                        {
                            bg_max[i] = sample;
                        }
                        bg_sum[i] += sample;
                        bg_squaredSum[i] += sample * sample;
                        i++;
                    }
                }
                bg_sampleCount++;
                if (bg_sampleCount == drawInterval)
                {
                    pictureBox.Invoke((MethodInvoker)UpdateBuffer);
                    ResetStats();
                }
            }
        }

        private void Process(IDspContext context, RecordingStartCommand command)
        {
            recording = true;
            messages.Add(Tuple.Create("REC" + Environment.NewLine + "[" + command.Number + "]", Color.FromArgb(240, 244, 67, 54)));
        }

        private void Process(IDspContext context, RecordingStopCommand command)
        {
            recording = false;
            messages.Add(Tuple.Create("STOP" + Environment.NewLine + "[" + command.Number + "]", Color.FromArgb(240, 244, 67, 54)));
        }

        private void UpdateBuffer()
        {
            ui_g.FillRectangle(background, ui_x, 0, 1, ui_buffer.Height);

            for (var ch = 0; ch < channelCount; ch++)
            {
                var min = bg_min[ch];
                var max = bg_max[ch];
                var mean = bg_sum[ch] / bg_sampleCount;
                var sd = Math.Sqrt(bg_squaredSum[ch] / bg_sampleCount - mean * mean);
                var sd1 = mean - sd;
                var sd2 = mean + sd;

                var top = (int)Math.Round((double)ui_buffer.Height * ch / channelCount);
                var bottom = (int)Math.Round((double)ui_buffer.Height * (ch + 1) / channelCount);
                var middle = (top + bottom) / 2;
                {
                    var y1 = (int)Math.Floor(top + (bottom - top) * (1 - max) / 2);
                    if (y1 < top) y1 = top;
                    if (y1 > bottom) y1 = bottom;
                    var y2 = (int)Math.Ceiling(top + (bottom - top) * (1 - min) / 2);
                    if (y2 < top) y2 = top;
                    if (y2 > bottom) y2 = bottom;
                    var rh = y2 - y1;
                    if (rh >= 1) ui_g.FillRectangle(waveform2[ch], ui_x, y1, 1, rh);
                    else ui_g.FillRectangle(waveform2[ch], ui_x, y1, 1, 1);
                }
                {
                    var y1 = (int)Math.Floor(top + (bottom - top) * (1 - sd2) / 2);
                    if (y1 < top) y1 = top;
                    if (y1 > bottom) y1 = bottom;
                    var y2 = (int)Math.Ceiling(top + (bottom - top) * (1 - sd1) / 2);
                    if (y2 < top) y2 = top;
                    if (y2 > bottom) y2 = bottom;
                    var rh = y2 - y1;
                    if (rh >= 1) ui_g.FillRectangle(waveform1[ch], ui_x, y1, 1, rh);
                    else ui_g.FillRectangle(waveform1[ch], ui_x, y1, 1, 1);
                }

                if (max >= 0.999F)
                {
                    ui_g.FillRectangle(clip, ui_x, top, 1, middle - top);
                }
                if (min <= -0.999F)
                {
                    var y = (int)Math.Floor(0.5F * top + 0.5F * bottom);
                    ui_g.FillRectangle(clip, ui_x, (bottom + top) / 2, 1, bottom - middle);
                }

                ui_g.FillRectangle(grid, ui_x, (top + bottom) / 2, 1, 1);
                ui_g.FillRectangle(grid, ui_x, bottom - 1, 1, 1);
            }

            ui_sampleCount += drawInterval;
            if (ui_sampleCount >= sampleRate)
            {
                ui_g.FillRectangle(grid, ui_x, 0, 1, ui_buffer.Height);
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
            pictureBox.Refresh();
        }

        private void Process(IDspContext context, KeyDownCommand command)
        {
            messages.Add(Tuple.Create(command.Value + Environment.NewLine, Color.FromArgb(240, 0, 188, 212)));
        }

        private void Process(IDspContext context, JumpingWarningCommand command)
        {
            messages.Add(Tuple.Create("JUMPING" + Environment.NewLine, Color.FromArgb(240, 255, 152, 0)));
        }

        public void Dispose()
        {
            pictureBox.Paint -= PictureBox_Paint;

            if (recordingColor != null)
            {
                recordingColor.Dispose();
                recordingColor = null;
            }

            if (font != null)
            {
                font.Dispose();
                font = null;
            }

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

            if (background != null)
            {
                background.Dispose();
                background = null;
            }
            if (grid != null)
            {
                grid.Dispose();
                grid = null;
            }
            if (clip != null)
            {
                clip.Dispose();
                clip = null;
            }
            if (waveform1 != null)
            {
                foreach (var brush in waveform1)
                {
                    if (brush != null)
                    {
                        brush.Dispose();
                    }
                }
                waveform1 = null;
            }
            if (waveform2 != null)
            {
                foreach (var brush in waveform2)
                {
                    if (brush != null)
                    {
                        brush.Dispose();
                    }
                }
                waveform2 = null;
            }
        }
    }
}
