using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Orcbolg.Dsp.Test
{
    static class Utilities
    {
        public static IReadOnlyList<IDictionary<string, int>> ReadTestCases(string filename)
        {
            using (var reader = new StreamReader(filename))
            {
                var names = reader.ReadLine().Split(',');
                var list = new List<Dictionary<string, int>>();
                for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
                {
                    var split = line.Split(',');
                    var dic = new Dictionary<string, int>();
                    for (var i = 0; i < names.Length; i++)
                    {
                        dic.Add(names[i], int.Parse(split[i]));
                    }
                    list.Add(dic);
                }
                return list;
            }
        }

        public static void AreEqual(float[] expected, float[] actual)
        {
            Assert.IsTrue(actual.Length == expected.Length);

            for (var i = 0; i < actual.Length; i++)
            {
                var d = Math.Abs(actual[i] - expected[i]);
                if (d >= 0.001)
                {
                    Assert.Fail("actual != expected");
                }
            }
        }

        public static void AreEqual(float[][] expected, float[][] actual)
        {
            for (var i = 0; i < actual.Length; i++)
            {
                AreEqual(expected[i], actual[i]);
            }
        }
    }
}
