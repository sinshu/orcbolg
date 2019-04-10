using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio.Wave;

namespace Orcbolg.Dsp
{
    public static class WaveFile
    {
        private static readonly int bufferLength = 1024;

        public static float[][] Read(string filename)
        {
            if (filename == null) throw new ArgumentNullException(nameof(filename));
            using (var reader = new WaveFileReader(filename))
            {
                var sampleOffset = 0;
                var sampleCount = (int)(reader.Length / reader.BlockAlign);
                return ReadSub(reader, sampleOffset, sampleCount);
            }
        }

        public static float[][] Read(string filename, out int sampleRate)
        {
            if (filename == null) throw new ArgumentNullException(nameof(filename));
            using (var reader = new WaveFileReader(filename))
            {
                sampleRate = reader.WaveFormat.SampleRate;
                var sampleOffset = 0;
                var sampleCount = (int)(reader.Length / reader.BlockAlign);
                return ReadSub(reader, sampleOffset, sampleCount);
            }
        }

        public static float[][] Read(string filename, int sampleOffset, int sampleCount)
        {
            if (filename == null) throw new ArgumentNullException(nameof(filename));
            if (sampleOffset < 0) throw new ArgumentException("Sample offset must be greater than or equal to zero.");
            if (sampleCount < 0) throw new ArgumentException("Sample count must be greater than or equal to zero.");
            using (var reader = new WaveFileReader(filename))
            {
                return ReadSub(reader, sampleOffset, sampleCount);
            }
        }

        public static float[][] Read(string filename, int sampleOffset, int sampleCount, out int sampleRate)
        {
            if (filename == null) throw new ArgumentNullException(nameof(filename));
            if (sampleOffset < 0) throw new ArgumentException("Sample offset must be greater than or equal to zero.");
            if (sampleCount < 0) throw new ArgumentException("Sample count must be greater than or equal to zero.");
            using (var reader = new WaveFileReader(filename))
            {
                sampleRate = reader.WaveFormat.SampleRate;
                return ReadSub(reader, sampleOffset, sampleCount);
            }
        }

        private static float[][] ReadSub(WaveFileReader reader, int sampleOffset, int sampleCount)
        {
            var dataLength = (int)(reader.Length / reader.BlockAlign);
            var endPosition = sampleOffset + sampleCount;
            if (endPosition > dataLength) throw new IndexOutOfRangeException("Sample offset or count is too big.");

            var processLength = endPosition - sampleOffset;

            var buffer = new byte[reader.BlockAlign * bufferLength];
            var destination = new float[reader.WaveFormat.Channels][];
            for (var ch = 0; ch < destination.Length; ch++)
            {
                destination[ch] = new float[processLength];
            }

            reader.Seek(reader.BlockAlign * sampleOffset, SeekOrigin.Begin);
            var currentPosition = 0;
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

        public static void Write(float[][] data, int sampleRate, string filename)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Any(x => x == null)) throw new ArgumentNullException("All channels must not be null.");

            Write(data, sampleRate, filename, 0, data[0].Length);
        }

        public static void Write(float[][] data, int sampleRate, string filename, int sampleOffset, int sampleCount)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Any(x => x == null)) throw new ArgumentNullException("All channels must not be null.");
            if (data.Any(x => x.Length != data[0].Length)) throw new ArgumentException("All channels must have the same length.");
            if (sampleRate <= 0) throw new ArgumentException("Sample rate must be greater than zero.");
            if (filename == null) throw new ArgumentNullException(nameof(filename));
            if (sampleOffset < 0) throw new ArgumentException("Sample offset must be greater than or equal to zero.");
            if (sampleCount < 0) throw new ArgumentException("Sample count must be greater than or equal to zero.");

            var format = new WaveFormat(sampleRate, 16, data.Length);
            using (var writer = new WaveFileWriter(filename, format))
            {
                var dataLength = data[0].Length;
                var endPosition = sampleOffset + sampleCount;
                if (endPosition > dataLength) throw new IndexOutOfRangeException("Sample offset or count is too big.");

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
