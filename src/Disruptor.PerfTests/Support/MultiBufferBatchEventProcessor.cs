﻿using System;
using System.Threading;

namespace Disruptor.PerfTests.Support
{
    public class MultiBufferBatchEventProcessor<T> : IEventProcessor
    {
        private volatile int _isRunning;
        private readonly IDataProvider<T>[] _providers;
        private readonly ISequenceBarrier[] _barriers;
        private readonly IEventHandler<T> _handler;
        private readonly Sequence[] _sequences;
        private long _count;

        public MultiBufferBatchEventProcessor(IDataProvider<T>[] providers, ISequenceBarrier[] barriers, IEventHandler<T> handler)
        {
            if (providers.Length != barriers.Length)
                throw new ArgumentException();

            _providers = providers;
            _barriers = barriers;
            _handler = handler;

            _sequences = new Sequence[providers.Length];
            for (var i = 0; i < _sequences.Length; i++)
            {
                _sequences[i] = new Sequence();
            }
        }

        public void Run()
        {
            if (Interlocked.Exchange(ref _isRunning, 1) != 0)
                throw new ApplicationException("Already running");

            foreach (var barrier in _barriers)
            {
                barrier.ClearAlert();
            }

            var barrierLength = _barriers.Length;

            while (true)
            {
                try
                {
                    for (var i = 0; i < barrierLength; i++)
                    {
                        var available = _barriers[i].WaitFor(-1);
                        var sequence = _sequences[i];

                        var nextSequence = sequence.Value + 1;

                        for (var l = nextSequence; l <= available; l++)
                        {
                            _handler.OnEvent(_providers[i][l], l, l == available);
                        }

                        sequence.SetValue(available);

                        _count += available - nextSequence + 1;
                    }

                    Thread.Yield();
                }
                catch (AlertException)
                {
                    if (_isRunning == 0)
                        break;
                }
                catch (TimeoutException e)
                {
                    Console.WriteLine(e);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    break;
                }
            }
        }

        

        public ISequence Sequence { get { throw new NotSupportedException(); } }

        public long Count => _count;

        public Sequence[] GetSequences()
        {
            return _sequences;
        }

        public void Halt()
        {
            _isRunning = 0;
            _barriers[0].Alert();
        }

        public bool IsRunning => _isRunning == 1;
    }
}