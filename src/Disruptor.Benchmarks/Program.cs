﻿using System;
using BenchmarkDotNet.Running;
using ObjectLayoutInspector;

namespace Disruptor.Benchmarks
{
    public static class Program
    {
        public static void Main()
        {
            //BenchmarkRunner.Run<RingBufferBenchmarks>();
            //BenchmarkRunner.Run<MultiProducerSequencerBenchmarks>();
            //BenchmarkRunner.Run<ObjectArrayBenchmarks>();
            //BenchmarkRunner.Run<Int32ArrayBenchmarks>();
            //BenchmarkRunner.Run<ValueArrayBenchmarks>();

            //TypeLayout.PrintLayout<Sequence>();
            //TypeLayout.PrintLayout<RingBuffer<object>>();
            //TypeLayout.PrintLayout<ValueRingBuffer<ValueRingBufferBenchmarks.Event>.PublishScope>();
            //TypeLayout.PrintLayout<ValueRingBuffer<ValueRingBufferBenchmarks.Event>.PublishScopeRange>();

            //RunMultiProducerSequencerBenchmarks();
            //RunInt32ArrayBenchmarks();
            //RunObjectArrayBenchmarks();
            //RunRingBufferBenchmarks();

            //Console.WriteLine(ObjectArrayBenchmarks.OffsetToArrayData);
            //Console.WriteLine(Int32ArrayBenchmarks.OffsetToArrayData);

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run();

            Console.ReadLine();
        }

        private static void RunMultiProducerSequencerBenchmarks()
        {
            var bench = new MultiProducerSequencerBenchmarks();

            bench.IsAvailable();
            bench.IsAvailablePointer();
            bench.Publish();
            bench.PublishPointer();

            Console.WriteLine("YYY");
            Console.ReadLine();
            Console.WriteLine("ZZZ");

            bench.IsAvailable();
            bench.IsAvailablePointer();
            bench.Publish();
            bench.PublishPointer();
        }

        private static void RunInt32ArrayBenchmarks()
        {
            var bench = new Int32ArrayBenchmarks();

            //bench.Write();
            //bench.WriteFixed();
            //bench.WritePointer();
            //bench.WriteUnsafe();
            bench.Read();
            bench.ReadFixed();

            Console.WriteLine("YYY");
            Console.ReadLine();

            //bench.Write();
            //bench.WriteFixed();
            //bench.WritePointer();
            //bench.WriteUnsafe();
            bench.Read();
            bench.ReadFixed();
        }
    }
}
