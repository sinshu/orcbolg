using System;
using System.Collections.Generic;
using System.Linq;
using Orcbolg.Dsp;
using Orcbolg.Recog;

static class Program
{
    private static void Main(string[] args)
    {
        int sampleRate;
        var data = WaveFile.Read("test.wav", out sampleRate);
        WaveFile.Write(data, sampleRate, "test_out.wav");
    }
}
