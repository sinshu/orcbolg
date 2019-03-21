using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Rockon
{
    internal class LoadMeter
    {
        private readonly PictureBox pictureBox;
        private readonly int meterCount;
        private readonly Func<double[]> source;
        private readonly double[] values;

        private Timer timer;

        public LoadMeter(PictureBox pictureBox, int meterCount, Func<double[]> source)
        {
            this.pictureBox = pictureBox;
            this.meterCount = meterCount;
            this.source = source;

            timer = new Timer();

            pictureBox.Paint += PictureBox_Paint;
        }

        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            var barLength = 60;
            for (var i = 0; i < meterCount; i++)
            {
                var y1 = (int)Math.Round(pictureBox.Height * (i + 0.25) / meterCount);
                var y2 = (int)Math.Round(pictureBox.Height * (i + 0.75) / meterCount);
                var barThickness = y2 - y1;
                e.Graphics.FillRectangle(Brushes.Black, 10, y1, barLength, barThickness);
            }
        }
    }
}
