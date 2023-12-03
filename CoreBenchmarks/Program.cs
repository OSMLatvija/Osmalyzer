using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;

namespace Osmalyzer.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<ChunkerBenchmark>();
    }
}

[Orderer(SummaryOrderPolicy.Method)]
public class ChunkerBenchmark
{
    [Params(30000, 100000, 300000)]
    public int ItemCount { get; set; } //= 300000;
    
    [Params(20, 30, 50)]
    public int ChunkCount { get; set; } //= 50;
    
    [Params(0.00003, 0.0003, 0.003)] 
    public double SeekDistance { get; set; } //= 0.0003;

    public int NumberOfLookups { get; set; } = 5000;
    
    // For example, for cultural heritage monuments:
    // There are 200K OSM elements that may match - these need to get chunked
    // We may look things up within 300 meters
    // So if Latvia is 450 km wide, that's a lookup "distance" of 0.3 km / 450 km / 2 or 0.00033 or 0.033 %
    // Monument data has 7000+ items
    // Each monument will lookup against OSM elements within that distance
    // So above parameters benchmark within an order of magnitude of that example
    
    // | Method        | ItemCount | ChunkCount | SeekDistance | Mean      | Error     | StdDev    |
    // |-------------- |---------- |----------- |------------- |----------:|----------:|----------:|
    // | BenchChunking | 30000     | 20         | 3E-05        |  1.764 ms | 0.0192 ms | 0.0180 ms |
    // | BenchChunking | 30000     | 20         | 0.0003       |  1.745 ms | 0.0057 ms | 0.0051 ms |
    // | BenchChunking | 30000     | 20         | 0.003        |  1.807 ms | 0.0332 ms | 0.0311 ms |
    // | BenchChunking | 30000     | 30         | 3E-05        |  1.874 ms | 0.0128 ms | 0.0120 ms |
    // | BenchChunking | 30000     | 30         | 0.0003       |  1.857 ms | 0.0222 ms | 0.0197 ms |
    // | BenchChunking | 30000     | 30         | 0.003        |  1.868 ms | 0.0306 ms | 0.0271 ms |
    // | BenchChunking | 30000     | 50         | 3E-05        |  1.965 ms | 0.0248 ms | 0.0232 ms |
    // | BenchChunking | 30000     | 50         | 0.0003       |  1.952 ms | 0.0232 ms | 0.0217 ms |
    // | BenchChunking | 30000     | 50         | 0.003        |  1.934 ms | 0.0139 ms | 0.0109 ms |
    // | BenchChunking | 100000    | 20         | 3E-05        |  7.372 ms | 0.1420 ms | 0.1744 ms |
    // | BenchChunking | 100000    | 20         | 0.0003       |  7.209 ms | 0.1272 ms | 0.1127 ms |
    // | BenchChunking | 100000    | 20         | 0.003        |  7.370 ms | 0.1457 ms | 0.1843 ms |
    // | BenchChunking | 100000    | 30         | 3E-05        |  7.416 ms | 0.0876 ms | 0.0819 ms |
    // | BenchChunking | 100000    | 30         | 0.0003       |  7.343 ms | 0.0672 ms | 0.0561 ms |
    // | BenchChunking | 100000    | 30         | 0.003        |  7.319 ms | 0.1366 ms | 0.1278 ms |
    // | BenchChunking | 100000    | 50         | 3E-05        |  9.324 ms | 0.1860 ms | 0.4121 ms |
    // | BenchChunking | 100000    | 50         | 0.0003       |  9.493 ms | 0.1873 ms | 0.4768 ms |
    // | BenchChunking | 100000    | 50         | 0.003        |  9.400 ms | 0.1873 ms | 0.3783 ms |
    // | BenchChunking | 300000    | 20         | 3E-05        | 23.359 ms | 0.4636 ms | 0.5694 ms |
    // | BenchChunking | 300000    | 20         | 0.0003       | 23.281 ms | 0.4440 ms | 0.4153 ms |
    // | BenchChunking | 300000    | 20         | 0.003        | 23.293 ms | 0.4382 ms | 0.4688 ms |
    // | BenchChunking | 300000    | 30         | 3E-05        | 27.419 ms | 0.4902 ms | 0.4345 ms |
    // | BenchChunking | 300000    | 30         | 0.0003       | 27.192 ms | 0.3443 ms | 0.3052 ms |
    // | BenchChunking | 300000    | 30         | 0.003        | 27.344 ms | 0.1902 ms | 0.1588 ms |
    // | BenchChunking | 300000    | 50         | 3E-05        | 25.865 ms | 0.4992 ms | 0.5342 ms |
    // | BenchChunking | 300000    | 50         | 0.0003       | 27.101 ms | 0.5330 ms | 0.7644 ms |
    // | BenchChunking | 300000    | 50         | 0.003        | 26.480 ms | 0.5249 ms | 0.8624 ms |
    
    // | BenchLookup   | 30000     | 20         | 3E-05        |  6.528 ms | 0.1300 ms | 0.1985 ms |
    // | BenchLookup   | 30000     | 20         | 0.0003       |  6.605 ms | 0.1290 ms | 0.1809 ms |
    // | BenchLookup   | 30000     | 20         | 0.003        |  6.972 ms | 0.0356 ms | 0.0298 ms |
    // | BenchLookup   | 30000     | 30         | 3E-05        |  4.891 ms | 0.0961 ms | 0.1468 ms |
    // | BenchLookup   | 30000     | 30         | 0.0003       |  4.823 ms | 0.0962 ms | 0.1709 ms |
    // | BenchLookup   | 30000     | 30         | 0.003        |  5.310 ms | 0.0471 ms | 0.0441 ms |
    // | BenchLookup   | 30000     | 50         | 3E-05        |  4.299 ms | 0.0854 ms | 0.2278 ms |
    // | BenchLookup   | 30000     | 50         | 0.0003       |  4.274 ms | 0.0847 ms | 0.2045 ms |
    // | BenchLookup   | 30000     | 50         | 0.003        |  4.632 ms | 0.0819 ms | 0.0766 ms |
    // | BenchLookup   | 100000    | 20         | 3E-05        | 18.293 ms | 0.3583 ms | 0.3352 ms |
    // | BenchLookup   | 100000    | 20         | 0.0003       | 18.534 ms | 0.2379 ms | 0.2226 ms |
    // | BenchLookup   | 100000    | 20         | 0.003        | 22.063 ms | 0.2994 ms | 0.2801 ms |
    // | BenchLookup   | 100000    | 30         | 3E-05        | 13.004 ms | 0.2577 ms | 0.3165 ms |
    // | BenchLookup   | 100000    | 30         | 0.0003       | 12.860 ms | 0.2477 ms | 0.3043 ms |
    // | BenchLookup   | 100000    | 30         | 0.003        | 15.881 ms | 0.3123 ms | 0.3596 ms |
    // | BenchLookup   | 100000    | 50         | 3E-05        | 12.868 ms | 0.2565 ms | 0.2953 ms |
    // | BenchLookup   | 100000    | 50         | 0.0003       | 13.151 ms | 0.2536 ms | 0.3115 ms |
    // | BenchLookup   | 100000    | 50         | 0.003        | 14.657 ms | 0.2710 ms | 0.2535 ms |
    // | BenchLookup   | 300000    | 20         | 3E-05        | 53.910 ms | 0.6080 ms | 0.5687 ms |
    // | BenchLookup   | 300000    | 20         | 0.0003       | 56.136 ms | 0.7476 ms | 0.6993 ms |
    // | BenchLookup   | 300000    | 20         | 0.003        | 64.062 ms | 1.2201 ms | 1.4051 ms |
    // | BenchLookup   | 300000    | 30         | 3E-05        | 41.878 ms | 0.8278 ms | 0.7743 ms |
    // | BenchLookup   | 300000    | 30         | 0.0003       | 41.557 ms | 0.8210 ms | 0.7679 ms |
    // | BenchLookup   | 300000    | 30         | 0.003        | 50.853 ms | 0.6777 ms | 0.6339 ms |
    // | BenchLookup   | 300000    | 50         | 3E-05        | 35.181 ms | 0.6956 ms | 0.7443 ms |
    // | BenchLookup   | 300000    | 50         | 0.0003       | 36.194 ms | 0.5613 ms | 0.5251 ms |
    // | BenchLookup   | 300000    | 50         | 0.003        | 42.317 ms | 0.6261 ms | 0.5550 ms |


    private List<TestItem> _items = null!;

    
    [GlobalSetup]
    public void Setup()
    {
        Random random = new Random(42);

        _items = new List<TestItem>();
        
        for (int i = 0; i < ItemCount; i++)
        {
            _items.Add(
                new TestItem(
                    random.NextDouble(),
                    random.NextDouble()
                )
            );
        }
    }

    
    [Benchmark]
    public int BenchChunking()
    {
        Chunker<TestItem> chunker = new Chunker<TestItem>(_items, true, ChunkCount);

        return chunker.Count;
    }
    
    [Benchmark]
    public int BenchLookup()
    {
        Chunker<TestItem> chunker = new Chunker<TestItem>(_items, true, ChunkCount);

        Random random = new Random(42);

        int dummyCount = 0;

        for (int i = 0; i < NumberOfLookups; i++)
        {
            List<TestItem> closest = chunker.GetAllClosest(
                (random.NextDouble(), random.NextDouble()),
                SeekDistance
            );

            dummyCount += closest.Count;
        }

        return dummyCount;
    }


    private class TestItem : IChunkerItem
    {
        public (double x, double y) ChunkCoord => (_x, _y);

        private readonly double _x;
        private readonly double _y;

        public TestItem(double x, double y)
        {
            _x = x;
            _y = y;
        }
    }
}