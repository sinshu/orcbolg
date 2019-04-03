using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio.Wave;

namespace Orcbolg.Dsp
{
    public static class WaveFile
    {
        private static readonly int bufferLength = 2357;

        public static float[][] Read(string filename, int sampleOffset, int sampleCount, out int sampleRate)
        {
            using (var reader = new WaveFileReader(filename))
            {
                sampleRate = reader.WaveFormat.SampleRate;

                var dataLength = (int)(reader.Length / reader.BlockAlign);
                var endPosition = sampleOffset + sampleCount;
                if (endPosition > dataLength)
                {
                    endPosition = dataLength;
                }
                var processLength = endPosition - sampleOffset;

                var buffer = new byte[reader.BlockAlign * bufferLength];
                var destination = new float[reader.WaveFormat.Channels][];
                for (var ch = 0; ch < destination.Length; ch++)
                {
                    destination[ch] = new float[processLength];
                }

                var currentPosition = 0;

                reader.Seek(reader.BlockAlign * sampleOffset, SeekOrigin.Begin);

                while (true)
                {
                    var restLength = processLength - currentPosition;
                    var readLength = Math.Min(bufferLength, restLength);
                    RwHelper.FillBuffer(reader, buffer, reader.BlockAlign * readLength);
                    RwHelper.ReadInt16(buffer, destination, currentPosition, readLength);
                    currentPosition += readLength;
                    if (currentPosition == processLength)
                    {
                        break;
                    }
                }

                return destination;
            }
        }

        public static void Write(float[][] data, string filename, int sampleRate, int sampleOffset, int sampleCount)
        {
            var format = new WaveFormat(sampleRate, 16, data.Length);
            using (var writer = new WaveFileWriter(filename, format))
            {
                var dataLength = data[0].Length;
                var endPosition = sampleOffset + sampleCount;
                if (endPosition > dataLength)
                {
                    endPosition = dataLength;
                }

                var buffer = new byte[format.BlockAlign * bufferLength];

                var currentPosition = sampleOffset;

                while (true)
                {
                    var restLength = endPosition - currentPosition;
                    var writeLength = Math.Min(bufferLength, restLength);
                    RwHelper.WriteInt16(data, buffer, currentPosition, writeLength);
                    writer.Write(buffer, 0, format.BlockAlign * writeLength);
                    currentPosition += writeLength;
                    if (currentPosition == endPosition)
                    {
                        break;
                    }
                }
            }
        }
    }
}
