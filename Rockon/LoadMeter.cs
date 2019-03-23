using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Rockon
{
    internal class LoadMeter
    {
        private readonly Form form;
        private readonly PictureBox pictureBox;
        private readonly string[] names;
        private readonly Func<double>[] sources;
        private readonly double[] values;

        private Timer timer;

        private Font font;

        public LoadMeter(Form form, PictureBox pictureBox, IReadOnlyList<string> names, IReadOnlyList<Func<double>> sources)
        {
            this.form = form;
            this.pictureBox = pictureBox;
            this.names = names.ToArray();
            this.sources = sources.ToArray();
            values = new double[this.names.Length];

            timer = new Timer();
            timer.Interval = 200;
            timer.Tick += Timer_Tick;

            pictureBox.Paint += PictureBox_Paint;

            font = new Font(FontFamily.GenericMonospace, 9);

            timer.Start();
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
                e.Graphics.DrawString(names[i], form.Font, Brushes.Black, 0, middle - form.Font.Height / 2);
                var barLength = Math.Max((int)Math.Ceiling((meterLength - 2) * values[i]), 1);
                e.Graphics.FillRectangle(Brushes.Black, meterLeft, y, meterLength, meterThickness);
                e.Graphics.FillRectangle(Brushes.Red, meterLeft + 1, y + 1, barLength, meterThickness - 2);
                var load = (int)Math.Ceiling(100 * values[i]) + " %";
                var w = e.Graphics.MeasureString(load, font).Width;
                e.Graphics.DrawString(load, font, Brushes.White, meterLeft + (meterLength - w) / 2, middle - font.Height / 2);
            }
        }
    }
}
