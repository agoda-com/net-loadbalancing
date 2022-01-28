using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace Agoda.Frameworks.LoadBalancing.Benchmark
{
    
    [CsvMeasurementsExporter, CsvExporter, RPlotExporter]
    public class UpdateWeightBenchmark
    {

        [Params(100)]
        public int numberOfRoutes;

        [Params(1000, 10000)]
        public int numberOfRuns;

        private Dictionary<string, WeightItem> Resource;
        private ResourceManager<string> ResourceManager;

        [GlobalSetup]
        public void Setup()
        {
            Resource = new Dictionary<string, WeightItem>();
            for (var i = 0; i < numberOfRoutes; i++)
            {
                Resource.Add($"url_{i}", WeightItem.CreateDefaultItem());
            }
            ResourceManager = new ResourceManager<string>(Resource.ToImmutableDictionary(), new AgodaWeightManipulationStrategy());
        }


        [Benchmark]
        public void UpdateWeight_Benchmark()
        {
            Parallel.For(0, numberOfRuns, (i) => {
                var rand = new Random();
                ResourceManager.UpdateWeight($"url_{rand.Next(0, numberOfRoutes)}", rand.NextDouble() >= 0.5);
            });
        }
    }
}
