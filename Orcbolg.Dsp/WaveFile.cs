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

        public static float[][] Read(string fileName)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            using (var reader = new WaveFileReader(fileName))
            {
                var sampleOffset = 0;
                var sampleCount = (int)(reader.Length / reader.BlockAlign);
                return ReadSub(reader, sampleOffset, sampleCount);
            }
        }

        public static float[][] Read(string fileName, out int sampleRate)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            using (var reader = new WaveFileReader(fileName))
            {
                sampleRate = reader.WaveFormat.SampleRate;
                var sampleOffset = 0;
                var sampleCount = (int)(reader.Length / reader.BlockAlign);
                return ReadSub(reader, sampleOffset, sampleCount);
            }
        }

        public static float[][] Read(string fileName, int sampleOffset, int sampleCount)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            if (sampleOffset < 0) throw new ArgumentOutOfRangeException("The sample offset must be greater than or equal to zero.", nameof(sampleOffset));
            if (sampleCount < 0) throw new ArgumentOutOfRangeException("The sample count must be greater than or equal to zero.", nameof(sampleCount));
            using (var reader = new WaveFileReader(fileName))
            {
                var dataLength = (int)(reader.Length / reader.BlockAlign);
                var endPosition = sampleOffset + sampleCount;
                if (endPosition > dataLength) throw new ArgumentOutOfRangeException("The sample offset or count is too big.");
                return ReadSub(reader, sampleOffset, sampleCount);
            }
        }

        public static float[][] Read(string fileName, int sampleOffset, int sampleCount, out int sampleRate)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            if (sampleOffset < 0) throw new ArgumentOutOfRangeException("The sample offset must be greater than or equal to zero.", nameof(sampleOffset));
            if (sampleCount < 0) throw new ArgumentOutOfRangeException("The sample count must be greater than or equal to zero.", nameof(sampleCount));
            using (var reader = new WaveFileReader(fileName))
            {
                var dataLength = (int)(reader.Length / reader.BlockAlign);
                var endPosition = sampleOffset + sampleCount;
                if (endPosition > dataLength) throw new ArgumentOutOfRangeException("The sample offset or count is too big.");
                sampleRate = reader.WaveFormat.SampleRate;
                return ReadSub(reader, sampleOffset, sampleCount);
            }
        }

        public static float[][] Read(string fileName, TimeSpan offset, TimeSpan length)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            using (var reader = new WaveFileReader(fileName))
            {
                var sampleOffset = (int)Math.Round(reader.WaveFormat.SampleRate * offset.TotalSeconds);
                var endPosition = (int)Math.Round(reader.WaveFormat.SampleRate * (offset.TotalSeconds + length.TotalSeconds));
                var sampleCount = endPosition - sampleOffset;
                if (sampleOffset < 0) throw new ArgumentOutOfRangeException("The offset must be greater than or equal to zero.", nameof(offset));
                if (sampleCount < 0) throw new ArgumentOutOfRangeException("The length must be greater than or equal to zero.", nameof(length));
                var dataLength = (int)(reader.Length / reader.BlockAlign);
                if (endPosition > dataLength) throw new ArgumentOutOfRangeException("The offset or length is too big.");
                return ReadSub(reader, sampleOffset, sampleCount);
            }
        }

        public static float[][] Read(string fileName, TimeSpan offset, TimeSpan length, out int sampleRate)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            using (var reader = new WaveFileReader(fileName))
            {
                var sampleOffset = (int)Math.Round(reader.WaveFormat.SampleRate * offset.TotalSeconds);
                var endPosition = (int)Math.Round(reader.WaveFormat.SampleRate * (offset.TotalSeconds + length.TotalSeconds));
                var sampleCount = endPosition - sampleOffset;
                if (sampleOffset < 0) throw new ArgumentOutOfRangeException("The offset must be greater than or equal to zero.", nameof(offset));
                if (sampleCount < 0) throw new ArgumentOutOfRangeException("The length must be greater than or equal to zero.", nameof(length));
                var dataLength = (int)(reader.Length / reader.BlockAlign);
                if (endPosition > dataLength) throw new ArgumentOutOfRangeException("The offset or length is too big.");
                sampleRate = reader.WaveFormat.SampleRate;
                return ReadSub(reader, sampleOffset, sampleCount);
            }
        }

        private static float[][] ReadSub(WaveFileReader reader, int sampleOffset, int sampleCount)
        {
            var dataLength = (int)(reader.Length / reader.BlockAlign);
            var endPosition = sampleOffset + sampleCount;
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

        public static void Write(float[][] data, int sampleRate, string fileName)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Any(x => x == null)) throw new ArgumentException("All the channels must not be null.", nameof(data));
            if (data.Any(x => x.Length != data[0].Length)) throw new ArgumentException("All the channels must have the same data length.", nameof(data));
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException("The sample rate must be greater than zero.", nameof(sampleRate));
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            WriteSub(data, sampleRate, fileName, 0, data[0].Length);
        }

        public static void Write(float[][] data, int sampleRate, string fileName, int sampleOffset, int sampleCount)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Any(x => x == null)) throw new ArgumentException("All the channels must not be null.", nameof(data));
            if (data.Any(x => x.Length != data[0].Length)) throw new ArgumentException("All the channels must have the same data length.", nameof(data));
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException("The sample rate must be greater than zero.", nameof(sampleRate));
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            if (sampleOffset < 0) throw new ArgumentOutOfRangeException("The sample offset must be greater than or equal to zero.", nameof(sampleOffset));
            if (sampleCount < 0) throw new ArgumentOutOfRangeException("The sample count must be greater than or equal to zero.", nameof(sampleCount));
            if (sampleOffset + sampleCount > data[0].Length) throw new ArgumentOutOfRangeException("The sample offset or count is too big.");
            WriteSub(data, sampleRate, fileName, sampleOffset, sampleCount);
        }

        public static void Write(float[][] data, int sampleRate, string fileName, TimeSpan offset, TimeSpan length)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Any(x => x == null)) throw new ArgumentException("All the channels must not be null.", nameof(data));
            if (data.Any(x => x.Length != data[0].Length)) throw new ArgumentException("All the channels must have the same data length.", nameof(data));
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException("The sample rate must be greater than zero.", nameof(sampleRate));
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            var sampleOffset = (int)Math.Round(sampleRate * offset.TotalSeconds);
            var endPosition = (int)Math.Round(sampleRate * (offset.TotalSeconds + length.TotalSeconds));
            if (endPosition > data[0].Length) throw new ArgumentOutOfRangeException("The offset or length is too big.");
            var sampleCount = endPosition - sampleOffset;
            WriteSub(data, sampleRate, fileName, sampleOffset, sampleCount);
        }

        private static void WriteSub(float[][] data, int sampleRate, string fileName, int sampleOffset, int sampleCount)
        {
            var format = new WaveFormat(sampleRate, 16, data.Length);
            using (var writer = new WaveFileWriter(fileName, format))
            {
                var dataLength = data[0].Length;
                var endPosition = sampleOffset + sampleCount;
                var buffer = new byte[format.BlockAlign * bufferLength];
                var currentPosition = 0;
                while (true)
                {
                    var restLength = sampleCount - currentPosition;
                    var writeLength = Math.Min(bufferLength, restLength);
                    RwHelper.WriteInt16(data, buffer, sampleOffset + currentPosition, writeLength);
                    writer.Write(buffer, 0, format.BlockAlign * writeLength);
                    currentPosition += writeLength;
                    if (currentPosition == sampleCount)
                    {
                        break;
                    }
                }
            }
        }
    }
}
