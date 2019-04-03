using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Orcbolg.Dsp
{
    internal static class RwHelper
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

        public static void ReadInt16(byte[] source, float[][] destination, int sampleOffset, int sampleCount)
        {
            unsafe
            {
                fixed (byte* bp = source)
                {
                    var sp = (short*)bp;
                    for (var t = 0; t < sampleCount; t++)
                    {
                        var shortOffset = destination.Length * t;
                        for (var ch = 0; ch < destination.Length; ch++)
                        {
                            var value = sp[shortOffset + ch];
                            destination[ch][sampleOffset + t] = (float)value / 0x8000;
                        }
                    }
                }
            }
        }

        public static void WriteInt16(float[][] source, byte[] destination, int sampleOffset, int sampleCount)
        {
            unsafe
            {
                fixed (byte* bp = destination)
                {
                    var sp = (short*)bp;
                    for (var t = 0; t < sampleCount; t++)
                    {
                        var shortOffset = source.Length * t;
                        for (var ch = 0; ch < source.Length; ch++)
                        {
                            var value = (int)Math.Round(0x8000 * source[ch][sampleOffset + t]);
                            if (value < short.MinValue)
                            {
                                value = short.MinValue;
                            }
                            else if (value > short.MaxValue)
                            {
                                value = short.MaxValue;
                            }
                            sp[shortOffset + ch] = (short)value;
                        }
                    }
                }
            }
        }
    }
}
