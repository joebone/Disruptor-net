﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Disruptor.PerfTests.Support
{
    /// <summary>
    /// An implementation of <see cref="TaskScheduler"/> which creates an underlying thread pool and set processor affinity to each thread.
    /// </summary>
    public sealed class RoundRobinThreadAffinedTaskScheduler : TaskScheduler, IDisposable
    {
        private List<Thread> _threads;
        private BlockingCollection<Task> _tasks;

        /// <summary>
        /// Create a new <see cref="RoundRobinThreadAffinedTaskScheduler"/> with a provided number of background threads.
        /// Threads are pined to a logical core using a round robin algorithm.
        /// </summary>
        /// <param name="numberOfThreads">Total number of threads in the pool.</param>
        public RoundRobinThreadAffinedTaskScheduler(int numberOfThreads)
        {
            if (numberOfThreads < 1)
                throw new ArgumentOutOfRangeException("numberOfThreads");

            var processorIndexes = Enumerable.Range(0, Environment.ProcessorCount).ToArray();

            CreateThreads(numberOfThreads, processorIndexes);
        }

        public static bool IsSupported { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// Create a new <see cref="RoundRobinThreadAffinedTaskScheduler"/> with a provided number of background threads.
        /// Threads are pined to a logical core using a round roubin algorithm choosen between provided processor indexes
        /// </summary>
        /// <param name="numberOfThreads">Total number of threads in the pool.</param>
        /// <param name="processorIndexes">One or more logical processor identifier(s) the threads are allowed to run on. 0-based indexes.</param>
        public RoundRobinThreadAffinedTaskScheduler(int numberOfThreads, params int[] processorIndexes)
        {
            if (numberOfThreads < 1)
                throw new ArgumentOutOfRangeException("numberOfThreads");

            foreach (var processorIndex in processorIndexes)
            {
                if (processorIndex >= Environment.ProcessorCount || processorIndex < 0)
                {
                    throw new ArgumentOutOfRangeException("processorIndexes", string.Format("processor index {0} was supperior to the total number of processors in the system", processorIndex));
                }
            }

            CreateThreads(numberOfThreads, processorIndexes);
        }

        private void CreateThreads(int numberOfThreads, int[] processorIndexes)
        {
            _tasks = new BlockingCollection<Task>();

            _threads = Enumerable.Range(0, numberOfThreads)
                                 .Select(i => new Thread(() => ThreadStartWithAffinity(i, processorIndexes)) { IsBackground = true })
                                 .ToList();

            _threads.ForEach(t => t.Start());
        }

        private void ThreadStartWithAffinity(int threadIndex, int[] processorIndexes)
        {
            var processorIndex = processorIndexes[threadIndex % processorIndexes.Length];

            using var affinityScope = ThreadAffinityUtil.SetThreadAffinity(processorIndex);

            foreach (var t in _tasks.GetConsumingEnumerable())
            {
                TryExecuteTask(t);
            }
        }

        /// <summary>
        /// Queues a <see cref="T:System.Threading.Tasks.Task"/> to the scheduler.
        /// </summary>
        /// <param name="task">The <see cref="T:System.Threading.Tasks.Task"/> to be queued.</param><exception cref="T:System.ArgumentNullException">The <paramref name="task"/> argument is null.</exception>
        protected override void QueueTask(Task task)
        {
            _tasks.Add(task);
        }

        /// <summary>
        /// Determines whether the provided <see cref="T:System.Threading.Tasks.Task"/> can be executed synchronously in this call, and if it can, executes it.
        /// </summary>
        /// <returns>
        /// A Boolean value indicating whether the task was executed inline.
        /// </returns>
        /// <param name="task">The <see cref="T:System.Threading.Tasks.Task"/> to be executed.</param><param name="taskWasPreviouslyQueued">A Boolean denoting whether or not task has previously been queued. If this parameter is True, then the task may have been previously queued (scheduled); if False, then the task is known not to have been queued, and this call is being made in order to execute the task inline without queuing it.</param><exception cref="T:System.ArgumentNullException">The <paramref name="task"/> argument is null.</exception><exception cref="T:System.InvalidOperationException">The <paramref name="task"/> was already executed.</exception>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }

        /// <summary>
        /// Generates an enumerable of <see cref="T:System.Threading.Tasks.Task"/> instances currently queued to the scheduler waiting to be executed.
        /// </summary>
        /// <returns>
        /// An enumerable that allows traversal of tasks currently queued to this scheduler.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">This scheduler is unable to generate a list of queued tasks at this time.</exception>
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _tasks.ToArray();
        }

        /// <summary>
        /// Indicates the maximum concurrency level this <see cref="T:System.Threading.Tasks.TaskScheduler"/> is able to support.
        /// </summary>
        /// <returns>
        /// Returns an integer that represents the maximum concurrency level.
        /// </returns>
        public override int MaximumConcurrencyLevel => _threads.Count;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            if (_tasks != null)
            {
                _tasks.CompleteAdding();

                _threads.ForEach(t => t.Join());

                _tasks.Dispose();
                _tasks = null;
            }
        }
    }
}
