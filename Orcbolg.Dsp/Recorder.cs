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

        private string dstWavPath;
        private string recWavPath;
        private WaveFileWriter wavWriter;
        private string dstCsvPath;
        private string recCsvPath;
        private StreamWriter csvWriter;
        private int bufferedSampleCount;
        private int processedSampleCount;
        private long recordingStartPosition;

        public Recorder(IDspDriver driver)
        {
            channelCount = driver.InputChannelCount;
            bufferLength = driver.SampleRate / 10;
            format = new WaveFormat(driver.SampleRate, driver.InputChannelCount);
            buffer = new byte[format.BlockAlign * bufferLength];

            dstWavPath = null;
            recWavPath = null;
            wavWriter = null;
            dstCsvPath = null;
            recCsvPath = null;
            csvWriter = null;
            bufferedSampleCount = 0;
            processedSampleCount = 0;
            recordingStartPosition = -1;
        }

        public void Dispose()
        {
            try
            {
                EndWriting();
            }
            catch
            {
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

            var keyDownCommand = command as KeyDownCommand;
            if (keyDownCommand != null)
            {
                Process(context, keyDownCommand);
            }

            var jumpingWarningCommand = command as JumpingWarningCommand;
            if (jumpingWarningCommand != null)
            {
                Process(context, jumpingWarningCommand);
            }
        }

        private void Process(IDspContext context, RecordingStartCommand command)
        {
            EndWriting();
            dstWavPath = command.Path;
            recWavPath = AddSuffix(dstWavPath, recordingSuffix);
            wavWriter = new WaveFileWriter(recWavPath, format);
            dstCsvPath = Path.ChangeExtension(command.Path, ".csv");
            recCsvPath = AddSuffix(dstCsvPath, recordingSuffix);
            csvWriter = new StreamWriter(recCsvPath);
            csvWriter.WriteLine("Position,Message");
            bufferedSampleCount = 0;
            processedSampleCount = 0;
            recordingStartPosition = -1;
        }

        private void Process(IDspContext context, RecordingStopCommand command)
        {
            EndWriting();
        }

        private void Process(IDspContext context, IntervalCommand command)
        {
            if (recordingStartPosition == -1)
            {
                recordingStartPosition = command.Position;
            }

            if (wavWriter != null)
            {
                unsafe
                {
                    fixed (byte* bp = buffer)
                    {
                        var sp = (short*)bp;
                        for (var t = 0; t < command.Length; t++)
                        {
                            var offset = channelCount * bufferedSampleCount;
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
                            bufferedSampleCount++;
                            if (bufferedSampleCount == bufferLength)
                            {
                                wavWriter.Write(buffer, 0, buffer.Length);
                                bufferedSampleCount = 0;
                            }
                        }
                    }
                }
            }

            processedSampleCount += command.Length;
        }

        private void Process(IDspContext context, KeyDownCommand command)
        {
            if (csvWriter != null)
            {
                csvWriter.WriteLine(processedSampleCount + ",Key_" + command.Value);
            }
        }

        private void Process(IDspContext context, JumpingWarningCommand command)
        {
            if (csvWriter != null && recordingStartPosition != -1)
            {
                var position = command.Position - recordingStartPosition;
                if (position >= 0)
                {
                    csvWriter.WriteLine(position + ",Jumping");
                }
            }
        }

        private void EndWriting()
        {
            if (wavWriter != null)
            {
                wavWriter.Dispose();
                wavWriter = null;
                File.Move(recWavPath, dstWavPath);
                dstWavPath = null;
                recWavPath = null;
            }
            if (csvWriter != null)
            {
                csvWriter.Dispose();
                csvWriter = null;
                File.Move(recCsvPath, dstCsvPath);
                dstCsvPath = null;
                recCsvPath = null;
            }
            bufferedSampleCount = 0;
            processedSampleCount = 0;
            recordingStartPosition = -1;
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
