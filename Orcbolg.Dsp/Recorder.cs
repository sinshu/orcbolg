using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio.Wave;

namespace Orcbolg.Dsp
{
    public sealed class Recorder : INonrealtimeDsp, IDisposable
    {
        private static readonly string recordingSuffix = "_recording";

        private readonly int channelCount;
        private readonly int bufferLength;
        private readonly WaveFormat format;
        private readonly byte[] buffer;

        private string destinationPath;
        private WaveFileWriter writer;
        private int sampleCount;

        public Recorder(IDspDriver driver)
        {
            channelCount = driver.InputChannelCount;
            bufferLength = driver.SampleRate / 10;
            format = new WaveFormat(driver.SampleRate, driver.InputChannelCount);
            buffer = new byte[format.BlockAlign * bufferLength];

            destinationPath = null;
            writer = null;
            sampleCount = 0;
        }

        public void Dispose()
        {
            if (writer != null)
            {
                EndWriting();
            }
        }

        public void Process(IDspContext context, IDspCommand command)
        {
            var recordingStartCommand = command as RecordingStartCommand;
            if (recordingStartCommand != null)
            {
                Process(context, recordingStartCommand);
            }

            var recordingStopCommand = command as RecordingStopCommand;
            if (recordingStopCommand != null)
            {
                Process(context, recordingStopCommand);
            }

            var intervalCommand = command as IntervalCommand;
            if (intervalCommand != null)
            {
                Process(context, intervalCommand);
            }
        }

        private void Process(IDspContext context, RecordingStartCommand command)
        {
            EndWriting();
            destinationPath = command.Path;
            writer = new WaveFileWriter(AddSuffix(destinationPath, recordingSuffix), format);
            sampleCount = 0;
        }

        private void Process(IDspContext context, RecordingStopCommand command)
        {
            EndWriting();
        }

        private void Process(IDspContext context, IntervalCommand command)
        {
            unsafe
            {
                if (writer != null)
                {
                    fixed (byte* bp = buffer)
                    {
                        var sp = (short*)bp;
                        for (var t = 0; t < command.Length; t++)
                        {
                            var offset = channelCount * sampleCount;
                            for (var ch = 0; ch < channelCount; ch++)
                            {
                                var value = (int)Math.Round(0x8000 * command.InputInterval[ch][t]);
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
                            sampleCount++;
                            if (sampleCount == bufferLength)
                            {
                                writer.Write(buffer, 0, buffer.Length);
                                sampleCount = 0;
                            }
                        }
                    }
                }
            }
        }

        private void EndWriting()
        {
            if (writer != null)
            {
                var recordingPath = writer.Filename;
                writer.Dispose();
                writer = null;
                File.Move(recordingPath, destinationPath);
            }
        }

        private static string AddSuffix(string path, string suffix)
        {
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileNameWithoutExtension(path);
            var extension = Path.GetExtension(path);
            return Path.Combine(directory, fileName + suffix + extension);
        }
    }

    public sealed class RecordingStartCommand : IDspCommand
    {
        private int number;
        private string path;

        public RecordingStartCommand(int number, string path)
        {
            this.number = number;
            this.path = path;
        }

        public int Number => number;
        public string Path => path;
    }

    public sealed class RecordingStopCommand : IDspCommand
    {
        private int number;

        public RecordingStopCommand(int number)
        {
            this.number = number;
        }

        public int Number => number;
    }

    public static class RecorderEx
    {
        public static void StartRecording(this IDspContext context, int number, string path)
        {
            context.Post(new RecordingStartCommand(number, path));
        }

        public static void StopRecording(this IDspContext context, int number)
        {
            context.Post(new RecordingStopCommand(number));
        }
    }
}
