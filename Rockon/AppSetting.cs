using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Rockon
{
    internal class AppSetting
    {
        private static readonly string defaultDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        private static readonly string defaultCfgName = "rockon.cfg";
        private static readonly char[] separators = new[] { ',' };

        private readonly string cfgPath;
        private readonly string driverName;
        private readonly int sampleRate;
        private readonly int bufferLength;
        private readonly int updateCycle;
        private readonly int[] inputChannels;
        private readonly int[] outputChannels;
        private readonly string recordingDirectory;

        public AppSetting() : this(Path.Combine(defaultDirectory, defaultCfgName))
        {
        }

        public AppSetting(string cfgPath)
        {
            this.cfgPath = cfgPath;

            var dic = Read(cfgPath);
            driverName = GetString(dic, "driver_name");
            sampleRate = GetInt(dic, "sample_rate_hz");
            bufferLength = sampleRate * GetInt(dic, "buffer_length_sec");
            updateCycle = GetInt(dic, "update_cycle");
            inputChannels = GetIntList(dic, "input_channels").Select(x => x - 1).ToArray();
            outputChannels = GetIntList(dic, "output_channels").Select(x => x - 1).ToArray();
            recordingDirectory = Path.Combine(defaultDirectory, GetString(dic, "rec_directory"));
        }

        private static Dictionary<string, LineInfo> Read(string path)
        {
            var dic = new Dictionary<string, LineInfo>();
            var pos = 1;
            foreach (var line in File.ReadLines(path, Encoding.Default))
            {
                var split = line.Split('=');
                if (split.Length == 2)
                {
                    var info = new LineInfo(split[0], split[1], pos);
                    dic[split[0]] = info;
                }
                pos++;
            }
            return dic;
        }

        private string GetString(Dictionary<string, LineInfo> dic, string key)
        {
            return dic[key].Value;
        }

        private int GetInt(Dictionary<string, LineInfo> dic, string key)
        {
            int value;
            LineInfo info;
            try
            {
                info = dic[key];
            }
            catch (KeyNotFoundException)
            {
                throw new KeyNotFoundException("設定項目 " + key + " がありません。(" + Path.GetFileName(cfgPath) + ")");
            }
            if (int.TryParse(info.Value, out value))
            {
                return value;
            }
            else
            {
                throw new FormatException("設定項目 " + key + " の値がおかしいです。(" + Path.GetFileName(cfgPath) + ", 行 " + info.Position + ")");
            }
        }

        private int[] GetIntList(Dictionary<string, LineInfo> dic, string key)
        {
            LineInfo info;
            try
            {
                info = dic[key];
            }
            catch (KeyNotFoundException)
            {
                throw new KeyNotFoundException("設定項目 " + key + " がありません。(" + Path.GetFileName(cfgPath) + ")");
            }
            var split = info.Value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            var values = new int[split.Length];
            for (var i = 0; i < split.Length; i++)
            {
                int value;
                if (int.TryParse(split[i], out value))
                {
                    values[i] = value;
                }
                else
                {
                    throw new FormatException("設定項目 " + key + " の値がおかしいです。(" + Path.GetFileName(cfgPath) + ", 行 " + info.Position + ")");
                }
            }
            return values;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(nameof(CfgPath)).Append(" = ").Append(CfgPath).AppendLine(",");
            sb.Append(nameof(DriverName)).Append(" = ").Append(DriverName).AppendLine(",");
            sb.Append(nameof(SampleRate)).Append(" = ").Append(SampleRate).AppendLine(",");
            sb.Append(nameof(BufferLength)).Append(" = ").Append(BufferLength).AppendLine(",");
            sb.Append(nameof(UpdateCycle)).Append(" = ").Append(UpdateCycle).AppendLine(",");
            sb.Append(nameof(InputChannels)).Append(" = { ").Append(string.Join(", ", InputChannels)).AppendLine(" },");
            sb.Append(nameof(OutputChannels)).Append(" = { ").Append(string.Join(", ", OutputChannels)).AppendLine(" },");
            sb.Append(nameof(RecordingDirectory)).Append(" = ").Append(RecordingDirectory).AppendLine();
            return sb.ToString();
        }

        public string CfgPath
        {
            get
            {
                return cfgPath;
            }
        }

        public string DriverName
        {
            get
            {
                return driverName;
            }
        }

        public int SampleRate
        {
            get
            {
                return sampleRate;
            }
        }

        public int BufferLength
        {
            get
            {
                return bufferLength;
            }
        }

        public int UpdateCycle
        {
            get
            {
                return updateCycle;
            }
        }

        public IReadOnlyList<int> InputChannels
        {
            get
            {
                return inputChannels;
            }
        }

        public IReadOnlyList<int> OutputChannels
        {
            get
            {
                return outputChannels;
            }
        }

        public string RecordingDirectory
        {
            get
            {
                return recordingDirectory;
            }
        }



        private class LineInfo
        {
            private string key;
            private string value;
            private int position;

            public LineInfo(string key, string value, int position)
            {
                this.key = key;
                this.value = value;
                this.position = position;
            }

            public override string ToString()
            {
                return "(" + key + ", " + value + ", " + position + ")";
            }

            public string Key
            {
                get
                {
                    return key;
                }
            }

            public string Value
            {
                get
                {
                    return value;
                }
            }

            public int Position
            {
                get
                {
                    return position;
                }
            }
        }
    }
}
