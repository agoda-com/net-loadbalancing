using BenchmarkDotNet.Running;
using System;

namespace Agoda.Frameworks.LoadBalancing.Benchmark
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<UpdateWeightBenchmark>();
        }
    }
}
