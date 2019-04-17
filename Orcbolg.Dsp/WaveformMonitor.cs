using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Forms;

namespace Orcbolg.Dsp
{
    public sealed class WaveformMonitor : INonrealtimeDsp, IDisposable
    {
        private readonly PictureBox pictureBox;
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

        private double[] bg_min;
        private double[] bg_max;
        private double[] bg_sum;
        private double[] bg_squaredSum;
        private int bg_sampleCount;

        private Brush backgroundColor;
        private Brush gridColor;
        private Brush clipColor;
        private Brush[] waveformColors1;
        private Brush[] waveformColors2;

        private Bitmap ui_buffer;
        private Graphics ui_g;
        private int ui_x;
        private int ui_sampleCount;
        private int ui_drawCycleCount;

        private long processedSampleCount;

        public WaveformMonitor(IDspDriver driver, PictureBox pictureBox, int updateInterval, int drawCycle, bool showOutput)
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

            try
            {
                this.pictureBox = pictureBox;
                this.updateInterval = updateInterval;
                this.drawCycle = drawCycle;
                this.showOutput = showOutput;

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

                bg_min = new double[channelCount];
                bg_max = new double[channelCount];
                bg_sum = new double[channelCount];
                bg_squaredSum = new double[channelCount];
                ResetSignalStats();

                backgroundColor = new SolidBrush(Color.FromArgb(33, 33, 33));
                gridColor = new SolidBrush(Color.FromArgb(64, 250, 250, 250));
                clipColor = new SolidBrush(Color.FromArgb(240, 244, 67, 54));
                waveformColors1 = new Brush[channelCount];
                waveformColors2 = new Brush[channelCount];
                {
                    var i = 0;
                    for (var ch = 0; ch < driver.InputChannelCount; ch++)
                    {
                        waveformColors1[i] = new SolidBrush(Color.FromArgb(0, 230, 118));
                        waveformColors2[i] = new SolidBrush(Color.FromArgb(192, 0, 230, 118));
                        i++;
                    }
                    if (showOutput)
                    {
                        for (var ch = 0; ch < driver.OutputChannelCount; ch++)
                        {
                            waveformColors1[i] = new SolidBrush(Color.FromArgb(0, 176, 255));
                            waveformColors2[i] = new SolidBrush(Color.FromArgb(192, 0, 176, 255));
                            i++;
                        }
                    }
                }

                Reset();

                processedSampleCount = 0;

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
                Process(context, intervalCommand);
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
                if (showOutput)
                {
                    for (var ch = 0; ch < command.OutputInterval.Length; ch++)
                    {
                        var sample = command.OutputInterval[ch][t];
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
                if (bg_sampleCount == updateInterval)
                {
                    pictureBox.Invoke((MethodInvoker)(() => UpdateBuffer(context)));
                    ResetSignalStats();
                }
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

        private void UpdateBuffer(IDspContext context)
        {
            ui_g.FillRectangle(backgroundColor, ui_x, 0, 1, ui_buffer.Height);

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
                    if (rh >= 1) ui_g.FillRectangle(waveformColors2[ch], ui_x, y1, 1, rh);
                    else ui_g.FillRectangle(waveformColors2[ch], ui_x, y1, 1, 1);
                }
                {
                    var y1 = (int)Math.Floor(top + (bottom - top) * (1 - sd2) / 2);
                    if (y1 < top) y1 = top;
                    if (y1 > bottom) y1 = bottom;
                    var y2 = (int)Math.Ceiling(top + (bottom - top) * (1 - sd1) / 2);
                    if (y2 < top) y2 = top;
                    if (y2 > bottom) y2 = bottom;
                    var rh = y2 - y1;
                    if (rh >= 1) ui_g.FillRectangle(waveformColors1[ch], ui_x, y1, 1, rh);
                    else ui_g.FillRectangle(waveformColors1[ch], ui_x, y1, 1, 1);
                }

                if (max >= 0.999F)
                {
                    ui_g.FillRectangle(clipColor, ui_x, top, 1, middle - top);
                }
                if (min <= -0.999F)
                {
                    ui_g.FillRectangle(clipColor, ui_x, (bottom + top) / 2, 1, bottom - middle);
                }

                ui_g.FillRectangle(gridColor, ui_x, (top + bottom) / 2, 1, 1);
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
            if (clipColor != null)
            {
                clipColor.Dispose();
                clipColor = null;
            }
            if (waveformColors1 != null)
            {
                foreach (var brush in waveformColors1)
                {
                    if (brush != null)
                    {
                        brush.Dispose();
                    }
                }
                waveformColors1 = null;
            }
            if (waveformColors2 != null)
            {
                foreach (var brush in waveformColors2)
                {
                    if (brush != null)
                    {
                        brush.Dispose();
                    }
                }
                waveformColors2 = null;
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
    }
}
