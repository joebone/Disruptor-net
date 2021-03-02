﻿using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Disruptor.Dsl;
using Disruptor.PerfTests.Support;

namespace Disruptor.PerfTests.External
{
    public class ThreeToOneQueueThroughputTest : IThroughputTest, IExternalTest
    {
        private const int _publisherCount = 3;
        private const int _bufferSize = 1024 * 64;
        private const long _iterations = 1000 * 1000 * 20;
        private readonly IExecutor _executor = new BasicExecutor(TaskScheduler.Current);
        private readonly Barrier _signal = new Barrier(_publisherCount + 1);
        private readonly ConcurrentQueue<long> _blockingQueue = new ConcurrentQueue<long>();
        private readonly PerfAdditionQueueEventProcessor _queueProcessor;
        private readonly PerfQueueProducer[] _perfQueueProducers = new PerfQueueProducer[_publisherCount];

        public ThreeToOneQueueThroughputTest()
        {
            _queueProcessor = new PerfAdditionQueueEventProcessor(_blockingQueue, ((_iterations / _publisherCount) * _publisherCount) - 1L);
            for (var i = 0; i < _publisherCount; i++)
            {
                _perfQueueProducers[i] = new PerfQueueProducer(_signal, _blockingQueue, _iterations / _publisherCount);
            }
        }

        public int RequiredProcessorCount => 4;

        public long Run(ThroughputSessionContext sessionContext)
        {
            var signal = new ManualResetEvent(false);
            _queueProcessor.Reset(signal);

            var tasks = new Task[_publisherCount];
            for (var i = 0; i < _publisherCount; i++)
            {
                tasks[i] = _executor.Execute(_perfQueueProducers[i].Run);
            }
            var processorTask = _executor.Execute(_queueProcessor.Run);

            sessionContext.Start();
            _signal.SignalAndWait();
            Task.WaitAll(tasks);
            signal.WaitOne();
            sessionContext.Stop();
            _queueProcessor.Halt();
            processorTask.Wait();

            return _iterations;
        }
    }
}