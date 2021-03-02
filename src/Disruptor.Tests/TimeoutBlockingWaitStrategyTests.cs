using System;
using System.Diagnostics;
using Disruptor.Tests.Support;
using NUnit.Framework;

namespace Disruptor.Tests
{
    [TestFixture]
    public class TimeoutBlockingWaitStrategyTests
    {
        [Test]
        public void ShouldTimeoutWaitFor()
        {
            var sequenceBarrier = new DummySequenceBarrier();

            var theTimeout = TimeSpan.FromMilliseconds(500);
            var waitStrategy = new TimeoutBlockingWaitStrategy(theTimeout);
            var cursor = new Sequence(5);
            var dependent = cursor;

            var stopwatch = Stopwatch.StartNew();

            Assert.Throws<TimeoutException>(() => waitStrategy.WaitFor(6, cursor, dependent, sequenceBarrier));

            stopwatch.Stop();

            // Required to make the test pass on azure pipelines.
            var tolerance = TimeSpan.FromMilliseconds(25);

            Assert.That(stopwatch.Elapsed, Is.GreaterThanOrEqualTo(theTimeout - tolerance));
        }
    }
}
