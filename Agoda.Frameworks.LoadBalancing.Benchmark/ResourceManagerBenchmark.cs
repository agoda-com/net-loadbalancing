using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Agoda.Frameworks.LoadBalancing.Benchmark
{
    
    [CsvMeasurementsExporter, CsvExporter, RPlotExporter]
    public class ResourceManagerBenchmark
    {

        [Params(100, 500, 1000)]
        public int weight;

        [Benchmark]
        public void UpdateWeight_Benchmark()
        {
            var dict = new Dictionary<string, WeightItem>
            {
                {"url1", new WeightItem(weight, 1000)}
            }.ToImmutableDictionary();

            var resourceManager = new ResourceManager<string>(dict, new AgodaWeightManipulationStrategy());
            resourceManager.UpdateWeight("url1", true);
        }

        [Benchmark]
        public void UpdateResource_Benchmark()
        {
            var urls = new List<string> { "url1", "url2", "url3", "url4" };
            var dict = urls.ToImmutableDictionary(x => x, x => WeightItem.CreateDefaultItem());
            var resourceManager = new ResourceManager<string>(dict, new AgodaWeightManipulationStrategy());
            resourceManager.UpdateResources(dict);
        }
    }
}
