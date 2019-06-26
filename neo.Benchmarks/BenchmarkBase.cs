using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Mathematics;

namespace Neo.Benchmarks
{
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    [RPlotExporter, RankColumn(NumeralSystem.Arabic)]
    public abstract class BenchmarkBase
    {
        public virtual void Setup()
        {
        }
    }
}