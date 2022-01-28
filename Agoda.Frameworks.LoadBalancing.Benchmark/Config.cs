using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using System;
using System.Collections.Generic;
using System.Text;

namespace Agoda.Frameworks.LoadBalancing.Benchmark
{
    public class Config : ManualConfig
    {
        public Config()
        {
            Add(CsvMeasurementsExporter.Default);
            Add(RPlotExporter.Default);
        }
    }
}
