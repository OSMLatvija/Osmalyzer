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
    //[Params(30000, 100000, 300000)]
    public int ItemCount { get; set; } = 300000;
    
    [Params(5, 10, 15, 20, 25, 30, 40, 50, 75, 100)]
    public int ChunkCount { get; set; }
    
    //[Params(0.00003, 0.0003, 0.003)] 
    public double SeekDistance { get; set; } = 0.0003;

    public int NumberOfLookups { get; set; } = 5000;
    
    // For example, for cultural heritage monuments:
    // There are 200K OSM elements that may match - these need to get chunked
    // We may look things up within 300 meters
    // So if Latvia is 450 km wide, that's a lookup "distance" of 0.3 km / 450 km / 2 or 0.00033 or 0.033 %
    // Monument data has 7000+ items
    // Each monument will lookup against OSM elements within that distance
    // So above parameters benchmark within an order of magnitude of that example
    
    // | Method        | ChunkCount | Mean      | Error    | StdDev   |
    // |-------------- |----------- |----------:|---------:|---------:|
    // | BenchChunking | 5          |  18.21 ms | 0.287 ms | 0.268 ms |
    // | BenchChunking | 10         |  17.27 ms | 0.331 ms | 0.418 ms |
    // | BenchChunking | 15         |  23.96 ms | 0.460 ms | 0.614 ms |
    // | BenchChunking | 20         |  23.15 ms | 0.460 ms | 0.492 ms |
    // | BenchChunking | 25         |  21.48 ms | 0.168 ms | 0.157 ms |
    // | BenchChunking | 30         |  27.54 ms | 0.421 ms | 0.373 ms |
    // | BenchChunking | 40         |  24.35 ms | 0.151 ms | 0.118 ms |
    // | BenchChunking | 50         |  25.86 ms | 0.505 ms | 0.757 ms |
    // | BenchChunking | 75         |  31.78 ms | 0.634 ms | 0.651 ms |
    // | BenchChunking | 100        |  35.40 ms | 0.373 ms | 0.349 ms |
    // | BenchLookup   | 5          | 486.46 ms | 0.996 ms | 0.931 ms |
    // | BenchLookup   | 10         | 135.08 ms | 0.299 ms | 0.280 ms |
    // | BenchLookup   | 15         |  76.78 ms | 1.496 ms | 1.470 ms |
    // | BenchLookup   | 20         |  55.60 ms | 1.034 ms | 0.916 ms |
    // | BenchLookup   | 25         |  45.50 ms | 0.311 ms | 0.291 ms |
    // | BenchLookup   | 30         |  41.48 ms | 0.792 ms | 0.740 ms |
    // | BenchLookup   | 40         |  36.42 ms | 0.549 ms | 0.513 ms |
    // | BenchLookup   | 50         |  35.78 ms | 0.566 ms | 0.530 ms |
    // | BenchLookup   | 75         |  36.61 ms | 0.453 ms | 0.424 ms |
    // | BenchLookup   | 100        |  33.79 ms | 0.659 ms | 0.902 ms |

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
        Chunker<TestItem> chunker = new Chunker<TestItem>(_items, ChunkCount);

        return chunker.Count;
    }
    
    [Benchmark]
    public int BenchLookup()
    {
        Chunker<TestItem> chunker = new Chunker<TestItem>(_items, ChunkCount);

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