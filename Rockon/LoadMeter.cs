using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Rockon
{
    internal class LoadMeter : IDisposable
    {
        private readonly Form form;
        private readonly PictureBox pictureBox;
        private readonly string[] names;
        private readonly Func<double>[] sources;
        private readonly double[] values;

        private Brush normalFore;
        private Brush recordingFore;
        private Brush greenBar;
        private Brush yellowBar;
        private Brush redBar;
        private Font font;

        private bool recording;

        private Timer timer;

        public LoadMeter(Form form, PictureBox pictureBox, IReadOnlyList<string> names, IReadOnlyList<Func<double>> sources)
        {
            try
            {
                this.form = form;
                this.pictureBox = pictureBox;
                this.names = names.ToArray();
                this.sources = sources.ToArray();
                values = new double[this.names.Length];

                normalFore = new SolidBrush(SystemColors.ControlText);
                recordingFore = new SolidBrush(Color.DarkRed);
                greenBar = new SolidBrush(Color.FromArgb(76, 175, 80));
                yellowBar = new SolidBrush(Color.FromArgb(255, 152, 0));
                redBar = new SolidBrush(Color.FromArgb(244, 67, 54));
                font = new Font(FontFamily.GenericMonospace, 9);

                timer = new Timer();
                timer.Interval = 100;
                timer.Tick += Timer_Tick;

                recording = false;

                pictureBox.Paint += PictureBox_Paint;

                timer.Start();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void SetRecordingState(bool recording)
        {
            this.recording = recording;
            pictureBox.Refresh();
        }

        private void UpdateValues()
        {
            for (var i = 0; i < values.Length; i++)
            {
                var value = sources[i]();
                if (value < 0) value = 0;
                if (value > 1) value = 1;
                values[i] = value;
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateValues();
            pictureBox.Refresh();
        }

        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            var fore = recording ? recordingFore : normalFore;

            var nameWidth = 0;
            foreach (var name in names)
            {
                var w = (int)Math.Ceiling(e.Graphics.MeasureString(name, form.Font).Width);
                if (w > nameWidth)
                {
                    nameWidth = w;
                }
            }

            var meterLeft = nameWidth + 2;
            var meterLength = pictureBox.Width - meterLeft;
            var meterThickness = (int)Math.Ceiling((double)pictureBox.Height / (2 * values.Length));
            for (var i = 0; i < values.Length; i++)
            {
                var y = (int)Math.Round(pictureBox.Height * (i + 0.25) / values.Length);
                var middle = y + meterThickness / 2;
                e.Graphics.DrawString(names[i], form.Font, fore, 0, middle - form.Font.Height / 2);
                var barLength = Math.Max((int)Math.Ceiling((meterLength - 2) * values[i]), 1);
                e.Graphics.FillRectangle(fore, meterLeft, y, meterLength, meterThickness);
                var loadValue = (int)Math.Ceiling(100 * values[i]);
                if (loadValue < 50)
                {
                    e.Graphics.FillRectangle(greenBar, meterLeft + 1, y + 1, barLength, meterThickness - 2);
                }
                else if (loadValue < 75)
                {
                    e.Graphics.FillRectangle(yellowBar, meterLeft + 1, y + 1, barLength, meterThickness - 2);
                }
                else
                {
                    e.Graphics.FillRectangle(redBar, meterLeft + 1, y + 1, barLength, meterThickness - 2);
                }
                var loadString = loadValue + " %";
                var w = e.Graphics.MeasureString(loadString, font).Width;
                e.Graphics.DrawString(loadString, font, Brushes.White, meterLeft + (meterLength - w) / 2, middle - font.Height / 2);
            }
        }

        public void Dispose()
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
                timer = null;
            }
            pictureBox.Paint -= PictureBox_Paint;
            if (normalFore != null)
            {
                normalFore.Dispose();
                normalFore = null;
            }
            if (recordingFore != null)
            {
                recordingFore.Dispose();
                recordingFore = null;
            }
            if (greenBar != null)
            {
                greenBar.Dispose();
                greenBar = null;
            }
            if (yellowBar != null)
            {
                yellowBar.Dispose();
                yellowBar = null;
            }
            if (redBar != null)
            {
                redBar.Dispose();
                redBar = null;
            }
            if (font != null)
            {
                font.Dispose();
                font = null;
            }
        }
    }
}
