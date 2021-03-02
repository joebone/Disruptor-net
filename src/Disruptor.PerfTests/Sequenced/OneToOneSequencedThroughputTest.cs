using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Disruptor.Dsl;
using Disruptor.PerfTests.Support;

namespace Disruptor.PerfTests.Sequenced
{
    /// <summary>
    /// UniCast a series of items between 1 publisher and 1 event processor.
    ///
    /// +----+    +-----+
    /// | P1 |--->| EP1 |
    /// +----+    +-----+
    ///
    /// Disruptor:
    /// ==========
    ///              track to prevent wrap
    ///              +------------------+
    ///              |                  |
    ///              |                  v
    /// +----+    +====+    +====+   +-----+
    /// | P1 |---\| RB |/---| SB |   | EP1 |
    /// +----+    +====+    +====+   +-----+
    ///      claim       get   ^        |
    ///                        |        |
    ///                        +--------+
    ///                          waitFor
    ///
    /// P1  - Publisher 1
    /// RB  - RingBuffer
    /// SB  - SequenceBarrier
    /// EP1 - EventProcessor 1
    /// </summary>
    public class OneToOneSequencedThroughputTest : IThroughputTest
    {
        private const int _bufferSize = 1024 * 64;
        private const long _iterations = 1000L * 1000L * 100L;

        private readonly RingBuffer<PerfEvent> _ringBuffer;
        private readonly AdditionEventHandler _eventHandler;
        private readonly long _expectedResult = PerfTestUtil.AccumulatedAddition(_iterations);
        private readonly IBatchEventProcessor<PerfEvent> _batchEventProcessor;
        private readonly IExecutor _executor = new BasicExecutor(TaskScheduler.Current);

        public OneToOneSequencedThroughputTest()
        {
            _eventHandler = new AdditionEventHandler();
            _ringBuffer = RingBuffer<PerfEvent>.CreateSingleProducer(PerfEvent.EventFactory, _bufferSize, new YieldingWaitStrategy());
            var sequenceBarrier = _ringBuffer.NewBarrier();
            _batchEventProcessor = BatchEventProcessorFactory.Create(_ringBuffer, sequenceBarrier, _eventHandler);
            _ringBuffer.AddGatingSequences(_batchEventProcessor.Sequence);
        }

        public int RequiredProcessorCount => 2;

        [MethodImpl(512)]
        public long Run(ThroughputSessionContext sessionContext)
        {
            long expectedCount = _batchEventProcessor.Sequence.Value + _iterations;

            _eventHandler.Reset(expectedCount);
            var processorTask = _executor.Execute(() => _batchEventProcessor.Run());

            _batchEventProcessor.WaitUntilStarted(TimeSpan.FromSeconds(5));

            sessionContext.Start();

            var ringBuffer = _ringBuffer;

            for (long i = 0; i < _iterations; i++)
            {
                var s = ringBuffer.Next();
                ringBuffer[s].Value = i;
                ringBuffer.Publish(s);
            }

            _eventHandler.WaitForSequence();
            sessionContext.Stop();
            PerfTestUtil.WaitForEventProcessorSequence(expectedCount, _batchEventProcessor);
            _batchEventProcessor.Halt();
            processorTask.Wait(2000);

            sessionContext.SetBatchData(_eventHandler.BatchesProcessed, _iterations);

            PerfTestUtil.FailIfNot(_expectedResult, _eventHandler.Value, $"Handler should have processed {_expectedResult} events, but was: {_eventHandler.Value}");

            return _iterations;
        }
    }
}
