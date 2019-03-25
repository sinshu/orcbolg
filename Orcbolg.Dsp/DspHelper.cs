using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Orcbolg.Dsp
{
    internal static class DspHelper
    {
        public static int FillBuffer(Stream stream, byte[] buffer, int count)
        {
            int read = 0;
            while (true)
            {
                var dr = stream.Read(buffer, read, count - read);
                if (dr == 0)
                {
                    return read;
                }

                read += dr;
                if (read == count)
                {
                    return read;
                }
            }
        }

        public static void ReadInt16(byte[] source, float[][] destination, int sampleCount)
        {
            unsafe
            {
                fixed (byte* bp = source)
                {
                    var sp = (short*)bp;
                    for (var t = 0; t < sampleCount; t++)
                    {
                        var offset = destination.Length * t;
                        for (var ch = 0; ch < destination.Length; ch++)
                        {
                            var value = sp[offset + ch];
                            destination[ch][t] = (float)value / 0x8000;
                        }
                    }
                }
            }
        }

        public static void WriteInt16(float[][] source, byte[] destination, int sampleCount)
        {
            unsafe
            {
                fixed (byte* bp = destination)
                {
                    var sp = (short*)bp;
                    for (var t = 0; t < sampleCount; t++)
                    {
                        var offset = source.Length * t;
                        for (var ch = 0; ch < source.Length; ch++)
                        {
                            var value = (int)Math.Round(0x8000 * source[ch][t]);
                            if (value < short.MinValue)
                            {
                                value = short.MinValue;
                            }
                            else if (value > short.MaxValue)
                            {
                                value = short.MaxValue;
                            }
                            sp[offset + ch] = (short)value;
                        }
                    }
                }
            }
        }
    }
}
