using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Orcbolg.Dsp
{
    internal sealed class DspScheduler
    {
        private readonly IDspContext context;
        private readonly DspBuffer buffer;

        private readonly ActionBlock<IDspCommand> actionBlock;
        private readonly DspThread[] threads;
        private readonly List<Exception> exceptions;

        private DspState state;

        private Task realtimeDspCompletion;
        private Task nonrealtimeDspCompletion;

        public DspScheduler(IDspContext context, DspBuffer buffer, IReadOnlyList<INonrealtimeDsp> dsps)
        {
            this.context = context;
            this.buffer = buffer;

            actionBlock = new ActionBlock<IDspCommand>(command => Process(command));
            threads = dsps.Select(dsp => new DspThread(context, dsp)).ToArray();
            exceptions = new List<Exception>();

            state = DspState.Initialized;
        }

        public void Start()
        {
            if (state != DspState.Initialized)
            {
                throw new InvalidOperationException("Start method must not be called more than once.");
            }

            state = DspState.Running;

            realtimeDspCompletion = Task.Run((Action)IntervalPolling);
            nonrealtimeDspCompletion = Run();
        }

        private void IntervalPolling()
        {
            try
            {
                var previous = DateTime.MaxValue;
                var threshold = TimeSpan.FromSeconds(3);

                while (state == DspState.Running)
                {
                    var entry = buffer.Read();
                    if (entry != null)
                    {
                        entry.SetReferenceCount(threads.Length + 1);
                        var command = new IntervalCommand(entry, buffer.IntervalLength);
                        actionBlock.Post(command);
                        entry.Release();
                        previous = DateTime.Now;
                    }
                    else
                    {
                        var now = DateTime.Now;
                        if ((now - previous) < threshold)
                        {
                            Thread.Sleep(1);
                        }
                        else
                        {
                            throw new DspException("Connection to the audio device timed out.");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                context.Stop(e);
            }
        }

        private async Task Run()
        {
            await realtimeDspCompletion;

            foreach (var thread in threads)
            {
                thread.Complete();
            }
            await Task.WhenAll(threads.Select(thread => thread.Completion));

            actionBlock.Complete();
            await actionBlock.Completion;

            if (exceptions.Count > 0)
            {
                throw new AggregateException("Exception was thrown while processing audio.", exceptions);
            }
        }

        public void Post(IDspCommand command)
        {
            actionBlock.Post(command);
        }

        private void Process(IDspCommand command)
        {
            var stopCommand = command as StopCommand;
            if (stopCommand != null)
            {
                state = DspState.Stop;
                if (stopCommand.Exception != null)
                {
                    exceptions.Add(stopCommand.Exception);
                }
                return;
            }

            if (state == DspState.Stop)
            {
                return;
            }

            foreach (var thread in threads)
            {
                thread.Post(command);
            }
        }

        public Task RealtimeDspCompletion
        {
            get
            {
                return realtimeDspCompletion;
            }
        }

        public Task NonrealtimeDspCompletion
        {
            get
            {
                return nonrealtimeDspCompletion;
            }
        }



        private sealed class DspThread
        {
            private readonly IDspContext context;
            private readonly INonrealtimeDsp dsp;

            private bool stopped;

            private readonly ActionBlock<IDspCommand> actionBlock;

            public DspThread(IDspContext context, INonrealtimeDsp dsp)
            {
                this.context = context;
                this.dsp = dsp;

                stopped = false;

                actionBlock = new ActionBlock<IDspCommand>((Action<IDspCommand>)Process);
            }

            public void Post(IDspCommand command)
            {
                actionBlock.Post(command);
            }

            private void Process(IDspCommand command)
            {
                try
                {
                    if (stopped)
                    {
                        return;
                    }

                    dsp.Process(context, command);
                    var intervalCommand = command as IntervalCommand;
                    if (intervalCommand != null)
                    {
                        intervalCommand.DspBufferEntry.Release();
                    }
                }
                catch (Exception e)
                {
                    stopped = true;
                    context.Stop(e);
                }
            }

            public void Complete()
            {
                actionBlock.Complete();
            }

            public Task Completion
            {
                get
                {
                    return actionBlock.Completion;
                }
            }
        }
    }
}
