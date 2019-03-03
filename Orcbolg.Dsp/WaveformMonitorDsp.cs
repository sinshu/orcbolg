using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Orcbolg.Dsp
{
    public sealed class WaveformMonitorDsp : INonrealtimeDsp, IDisposable
    {
        private readonly IDspDriver driver;
        private readonly PictureBox pictureBox;
        private readonly int drawInterval;
        private readonly bool drawOutput;

        private readonly int channelCount;
        private readonly int sampleRate;

        private readonly List<Tuple<string, Color>> tuples;

        private double[] bgt_min;
        private double[] bgt_max;
        private double[] bgt_sum;
        private double[] bgt_squaredSum;
        private int bgt_sampleCount;

        private Brush background;
        private Brush grid;
        private Brush clip;
        private Brush[] waveform1;
        private Brush[] waveform2;

        private Bitmap uit_buffer;
        private Graphics uit_g;
        private int uit_x;
        private int uit_sampleCount;

        public WaveformMonitorDsp(IDspDriver driver, PictureBox pictureBox, int drawInterval, bool drawOutput)
        {
            try
            {
                this.driver = driver;
                this.pictureBox = pictureBox;
                this.drawInterval = drawInterval;
                this.drawOutput = drawOutput;

                channelCount = drawOutput ? driver.InputChannelCount + driver.OutputChannelCount : driver.InputChannelCount;
                sampleRate = driver.SampleRate;

                tuples = new List<Tuple<string, Color>>();

                bgt_min = new double[channelCount];
                bgt_max = new double[channelCount];
                bgt_sum = new double[channelCount];
                bgt_squaredSum = new double[channelCount];
                ResetStats();

                background = new SolidBrush(Color.FromArgb(33, 33, 33));
                grid = new SolidBrush(Color.FromArgb(32, 250, 250, 250));
                clip = new SolidBrush(Color.FromArgb(244, 67, 54));
                waveform1 = new Brush[channelCount];
                waveform2 = new Brush[channelCount];
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
            e.Graphics.DrawImage(uit_buffer, 0, 0);
        }

        private void ResetStats()
        {
            for (var ch = 0; ch < channelCount; ch++)
            {
                bgt_min[ch] = double.MaxValue;
                bgt_max[ch] = double.MinValue;
                Array.Clear(bgt_sum, 0, bgt_sum.Length);
                Array.Clear(bgt_squaredSum, 0, bgt_squaredSum.Length);
            }
            bgt_sampleCount = 0;
        }

        public void Reset()
        {
            if (uit_buffer != null)
            {
                uit_g.Dispose();
                uit_buffer.Dispose();
            }
            uit_buffer = new Bitmap(pictureBox.Width, pictureBox.Height);
            uit_g = Graphics.FromImage(uit_buffer);
            uit_x = 0;
            uit_sampleCount = 0;
        }

        public void Process(IDspContext context, IDspCommand command)
        {
            var intervalCommand = command as IntervalCommand;
            if (intervalCommand != null)
            {
                Process(context, intervalCommand);
            }

            var jumpinessWarningCommand = command as JumpinessWarningCommand;
            if (jumpinessWarningCommand != null)
            {
                Process(jumpinessWarningCommand);
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
                    if (sample < bgt_min[i])
                    {
                        bgt_min[i] = sample;
                    }
                    if (sample > bgt_max[i])
                    {
                        bgt_max[i] = sample;
                    }
                    bgt_sum[i] += sample;
                    bgt_squaredSum[i] += sample * sample;
                    i++;
                }
                if (drawOutput)
                {
                    for (var ch = 0; ch < command.OutputInterval.Length; ch++)
                    {
                        var sample = command.InputInterval[ch][t];
                        if (sample < bgt_min[i])
                        {
                            bgt_min[i] = sample;
                        }
                        if (sample > bgt_max[i])
                        {
                            bgt_max[i] = sample;
                        }
                        bgt_sum[i] += sample;
                        bgt_squaredSum[i] += sample * sample;
                        i++;
                    }
                }
                bgt_sampleCount++;
                if (bgt_sampleCount == drawInterval)
                {
                    pictureBox.Invoke((MethodInvoker)UpdateBuffer);
                    ResetStats();
                }
            }
        }

        private void UpdateBuffer()
        {
            uit_g.FillRectangle(background, uit_x, 0, 1, uit_buffer.Height);

            for (var ch = 0; ch < channelCount; ch++)
            {
                var min = bgt_min[ch];
                var max = bgt_max[ch];
                var mean = bgt_sum[ch] / bgt_sampleCount;
                var sd = Math.Sqrt(bgt_squaredSum[ch] / bgt_sampleCount - mean * mean);
                var sd1 = mean - sd;
                var sd2 = mean + sd;

                var top = (int)Math.Round((double)uit_buffer.Height * ch / channelCount);
                var bottom = (int)Math.Round((double)uit_buffer.Height * (ch + 1) / channelCount);
                var middle = (top + bottom) / 2;
                {
                    var y1 = (int)Math.Floor(top + (bottom - top) * (1 - max) / 2);
                    if (y1 < top) y1 = top;
                    if (y1 > bottom) y1 = bottom;
                    var y2 = (int)Math.Ceiling(top + (bottom - top) * (1 - min) / 2);
                    if (y2 < top) y2 = top;
                    if (y2 > bottom) y2 = bottom;
                    var rh = y2 - y1;
                    if (rh >= 1) uit_g.FillRectangle(waveform2[ch], uit_x, y1, 1, rh);
                    else uit_g.FillRectangle(waveform2[ch], uit_x, y1, 1, 1);
                }
                {
                    var y1 = (int)Math.Floor(top + (bottom - top) * (1 - sd2) / 2);
                    if (y1 < top) y1 = top;
                    if (y1 > bottom) y1 = bottom;
                    var y2 = (int)Math.Ceiling(top + (bottom - top) * (1 - sd1) / 2);
                    if (y2 < top) y2 = top;
                    if (y2 > bottom) y2 = bottom;
                    var rh = y2 - y1;
                    if (rh >= 1) uit_g.FillRectangle(waveform1[ch], uit_x, y1, 1, rh);
                    else uit_g.FillRectangle(waveform1[ch], uit_x, y1, 1, 1);
                }

                if (max >= 0.999F)
                {
                    uit_g.FillRectangle(clip, uit_x, top, 1, middle - top);
                }
                if (min <= -0.999F)
                {
                    var y = (int)Math.Floor(0.5F * top + 0.5F * bottom);
                    uit_g.FillRectangle(clip, uit_x, (bottom + top) / 2, 1, bottom - middle);
                }

                uit_g.FillRectangle(grid, uit_x, (top + bottom) / 2, 1, 1);
                uit_g.FillRectangle(grid, uit_x, bottom - 1, 1, 1);
            }

            uit_sampleCount += drawInterval;
            if (uit_sampleCount >= sampleRate)
            {
                uit_g.FillRectangle(grid, uit_x, 0, 1, uit_buffer.Height);
                uit_sampleCount -= sampleRate;
            }

            uit_x++;
            if (uit_x == uit_buffer.Width)
            {
                uit_x = 0;
            }
            pictureBox.Refresh();
        }

        private void Process(JumpinessWarningCommand command)
        {

        }

        public void Dispose()
        {
            pictureBox.Paint -= PictureBox_Paint;

            if (uit_g != null)
            {
                uit_g.Dispose();
                uit_g = null;
            }
            if (uit_buffer != null)
            {
                uit_buffer.Dispose();
                uit_buffer = null;
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
            }
        }
    }
}
