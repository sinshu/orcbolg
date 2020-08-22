using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using NAudio.Wave;

namespace Orcbolg.Dsp
{
    public sealed class WaveRecorder : INonrealtimeDsp, IDisposable
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
        private long desiredRecordingSampleCount;
        private int bufferedSampleCount;
        private long recordedSampleCount;
        private long recordingStartPosition;

        private long processedSampleCount;

        private Stopwatch timer;
        private object mutex;
        private double maxTime;
        private double weight;
        private double dspTime;
        private double cpuLoad;

        public WaveRecorder(IDspDriver driver)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));

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
            desiredRecordingSampleCount = 0;
            bufferedSampleCount = 0;
            recordedSampleCount = 0;
            recordingStartPosition = -1;

            processedSampleCount = 0;

            timer = new Stopwatch();
            mutex = new object();
            maxTime = (double)driver.IntervalLength / driver.SampleRate;
            weight = Math.Pow(10, -3 / ((double)driver.SampleRate / driver.IntervalLength));
            dspTime = 0;
            cpuLoad = 0;
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

            var recordingAbortCommand = command as RecordingAbortCommand;
            if (recordingAbortCommand != null)
            {
                Process(context, recordingAbortCommand);
            }

            var intervalCommand = command as IntervalCommand;
            if (intervalCommand != null)
            {
                timer.Start();

                Process(context, intervalCommand);

                timer.Stop();
                var epalsed = timer.Elapsed.TotalSeconds;
                dspTime = weight * dspTime + (1 - weight) * epalsed;
                var newCpuLoad = Math.Min(dspTime / maxTime, 1.0);
                lock (mutex)
                {
                    cpuLoad = newCpuLoad;
                }
                timer.Reset();
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
            csvWriter = new StreamWriter(recCsvPath, false, Encoding.Default);
            csvWriter.WriteLine("Position,Message");
            desiredRecordingSampleCount = command.SampleCount;
            bufferedSampleCount = 0;
            recordedSampleCount = 0;
            recordingStartPosition = -1;
        }

        private void Process(IDspContext context, RecordingAbortCommand command)
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
                            recordedSampleCount++;
                            if (recordedSampleCount == desiredRecordingSampleCount)
                            {
                                context.Post(new RecordingCompleteCommand());
                                if (bufferedSampleCount > 0)
                                {
                                    wavWriter.Write(buffer, 0, format.BlockAlign * bufferedSampleCount);
                                }
                                EndWriting();
                                break;
                            }
                        }
                    }
                }
            }

            Interlocked.Add(ref processedSampleCount, command.Length);
        }

        private void Process(IDspContext context, KeyDownCommand command)
        {
            if (csvWriter != null && recordingStartPosition != -1)
            {
                var position = processedSampleCount - recordingStartPosition;
                csvWriter.WriteLine(position + ",Key_" + command.Value);
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
            desiredRecordingSampleCount = 0;
            bufferedSampleCount = 0;
            recordedSampleCount = 0;
            recordingStartPosition = -1;
        }

        private static string AddSuffix(string path, string suffix)
        {
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileNameWithoutExtension(path);
            var extension = Path.GetExtension(path);
            return Path.Combine(directory, fileName + suffix + extension);
        }

        public long ProcessedSampleCount
        {
            get
            {
                return Interlocked.Read(ref processedSampleCount);
            }
        }

        public double CpuLoad
        {
            get
            {
                lock (mutex)
                {
                    return cpuLoad;
                }
            }
        }
    }



    public sealed class RecordingStartCommand : IDspCommand
    {
        private int number;
        private string path;
        private long sampleCount;

        public RecordingStartCommand(int number, string path, long sampleCount)
        {
            this.number = number;
            this.path = path;
            this.sampleCount = sampleCount;
        }

        public int Number => number;
        public string Path => path;
        public long SampleCount => sampleCount;
    }



    public sealed class RecordingCompleteCommand : IDspCommand
    {
        public RecordingCompleteCommand()
        {
        }
    }



    public sealed class RecordingAbortCommand : IDspCommand
    {
        public RecordingAbortCommand()
        {
        }
    }



    public static class RecorderEx
    {
        public static void StartRecording(this IDspContext context, int number, string path)
        {
            context.Post(new RecordingStartCommand(number, path, long.MaxValue));
        }

        public static void StartRecording(this IDspContext context, int number, string path, long sampleCount)
        {
            context.Post(new RecordingStartCommand(number, path, sampleCount));
        }

        public static void AbortRecording(this IDspContext context)
        {
            context.Post(new RecordingAbortCommand());
        }
    }
}
