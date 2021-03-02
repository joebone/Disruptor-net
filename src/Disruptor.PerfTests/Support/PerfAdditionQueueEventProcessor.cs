using System.Collections.Concurrent;
using System.Threading;

namespace Disruptor.PerfTests.Support
{
    public class PerfAdditionQueueEventProcessor
    {
        private volatile bool _running;
        private long _value;
        private long _sequence;
        private ManualResetEvent _signal;

        private readonly ConcurrentQueue<long> _queue;
        private readonly long _count;

        public PerfAdditionQueueEventProcessor(ConcurrentQueue<long> queue, long count)
        {
            _queue = queue;
            _count = count;
        }

        public long Value => _value;

        public void Reset(ManualResetEvent signal)
        {
            _value = 0L;
            _sequence = 0L;
            _signal = signal;
        }

        public void Halt() => _running = false;

        public void Run()
        {
            _running = true;
            while (_running)
            {
                long value;
                if (!_queue.TryDequeue(out value))
                    continue;

                _value += value;

                if (_sequence++ == _count)
                {
                    _signal.Set();
                }
            }
        }
    }
}
