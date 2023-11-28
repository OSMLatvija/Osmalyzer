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
    public int ItemCount { get; set; }
    
    [Params(0.00001, 0.0001, 0.001)] 
    public double SeekDistance { get; set; }

    public int NumberOfLookups { get; set; } = 5000;
    
    // For example, for cultural heritage monuments:
    // There are 200K OSM elements that may match - these need to get chunked
    // We may look things up within 300 meters
    // So if Latvia is 450 km wide, that's a lookup "distance" of 0.3 km / 450 km / 2 or 0.00033 or 0.033 %
    // Monument data has 7000+ items
    // Each monument will lookup against OSM elements within that distance
    // So above parameters benchmark within an order of magnitude of that example
    
    // | Method        | ItemCount | SeekDistance | Mean      | Error     | StdDev    |
    // |-------------- |---------- |------------- |----------:|----------:|----------:|
    // | BenchChunking | 30000     | 1E-05        |  1.686 ms | 0.0044 ms | 0.0041 ms |
    // | BenchChunking | 30000     | 0.0001       |  1.661 ms | 0.0061 ms | 0.0057 ms |
    // | BenchChunking | 30000     | 0.001        |  1.666 ms | 0.0063 ms | 0.0059 ms |
    // | BenchChunking | 100000    | 1E-05        |  6.768 ms | 0.1296 ms | 0.1273 ms |
    // | BenchChunking | 100000    | 0.0001       |  6.803 ms | 0.1308 ms | 0.1454 ms |
    // | BenchChunking | 100000    | 0.001        |  6.729 ms | 0.1219 ms | 0.1081 ms |
    // | BenchChunking | 300000    | 1E-05        | 29.463 ms | 0.5824 ms | 0.8536 ms |
    // | BenchChunking | 300000    | 0.0001       | 29.740 ms | 0.5367 ms | 0.5020 ms |
    // | BenchChunking | 300000    | 0.001        | 29.551 ms | 0.5687 ms | 0.5585 ms |
    // | BenchLookup   | 30000     | 1E-05        |  7.401 ms | 0.1037 ms | 0.0919 ms |
    // | BenchLookup   | 30000     | 0.0001       |  7.546 ms | 0.1148 ms | 0.1074 ms |
    // | BenchLookup   | 30000     | 0.001        |  7.748 ms | 0.1136 ms | 0.1062 ms |
    // | BenchLookup   | 100000    | 1E-05        | 22.936 ms | 0.4566 ms | 0.5607 ms |
    // | BenchLookup   | 100000    | 0.0001       | 22.696 ms | 0.4511 ms | 0.4633 ms |
    // | BenchLookup   | 100000    | 0.001        | 23.905 ms | 0.4779 ms | 0.4470 ms |
    // | BenchLookup   | 300000    | 1E-05        | 74.889 ms | 1.0476 ms | 0.9799 ms |
    // | BenchLookup   | 300000    | 0.0001       | 75.778 ms | 0.7185 ms | 0.6721 ms |
    // | BenchLookup   | 300000    | 0.001        | 75.698 ms | 0.6309 ms | 0.5902 ms |
    
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
        Chunker<TestItem> chunker = new Chunker<TestItem>(_items);

        return chunker.Count;
    }
    
    [Benchmark]
    public int BenchLookup()
    {
        Chunker<TestItem> chunker = new Chunker<TestItem>(_items);

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