using System;
using System.Linq;

namespace Osmalyzer;

/// <summary>
/// Welcome to Chunks®, please choose your Chunks®.
/// Provides efficient spatial lookup of items. 
/// </summary>
public class Chunker<T> where T : IChunkerItem
{
    public int Count => _elements.Count;

    
    private readonly List<(T, (double, double))> _elements;

    private readonly int _span; 
    
    private readonly double _minX; 
    private readonly double _minY; 
    private readonly double _maxX; 
    private readonly double _maxY; 
    
    private readonly double _size;
    
    private readonly double _chunkSize;

    private Chunk?[,]? _chunks;


    public Chunker(IList<T> items, bool alwaysChunk = false, int chunkCountPerDimension = 50)
    {
        _span = chunkCountPerDimension; 

        _elements = new List<(T, (double, double))>(items.Count);
        
        _minX = double.MaxValue;
        _minY = double.MaxValue;
        _maxX = double.MinValue;
        _maxY = double.MinValue;
        
        foreach (T item in items)
        {
            (double x, double y) coord = item.ChunkCoord;
            
            _elements.Add((item, coord));

            _minX = Math.Min(coord.x, _minX);
            _minY = Math.Min(coord.y, _minY);
            _maxX = Math.Max(coord.x, _maxX);
            _maxY = Math.Max(coord.y, _maxY);
        }

        if (_minX >= _maxX) _maxX += 10f;
        if (_minY >= _maxY) _maxY += 10f;

        _size = _maxX - _minX;

        _chunkSize = _size / _span;
        
        if (alwaysChunk) // By default, we don't make chunks yet - until first lookup, we may not even need to
            ChunkItUp();
    }


    [Pure]
    [PublicAPI]
    public T? GetClosest((double x, double y) target, double? maxDistance)
    {
        return 
            maxDistance == null ? 
                GetClosest(target) : 
                GetClosest(target, maxDistance.Value);
    }

    [Pure]
    [PublicAPI]
    public T? GetClosest((double x, double y) target)
    {
        if (_elements.Count > 100 || _chunks != null) // no point with fewer because overhead is likely to exceed the individual search speed-up (unless we already chunked it up)
            return GetClosestChunked(target);

        return GetClosestManually(target, null);
    }

    [Pure]
    [PublicAPI]
    public T? GetClosest((double x, double y) target, double maxDistance)
    {
        if (_elements.Count > 100 || _chunks != null) // no point with fewer because overhead is likely to exceed the individual search speed-up (unless we already chunked it up)
            return GetClosestChunked(target, maxDistance);

        return GetClosestManually(target, maxDistance);
    }

    [Pure]
    [PublicAPI]
    public List<T> GetAllClosest((double x, double y) target, double? maxDistance)
    {
        return 
            maxDistance == null ? 
                GetAllClosest(target) : 
                GetAllClosest(target, maxDistance.Value);
    }

    [Pure]
    [PublicAPI]
    public List<T> GetAllClosest((double x, double y) target)
    {
        // Without max distance, there is no point doing chunked logic - we will end up covering the entire grid anyway 
        
        return GetAllClosestManually(target);
    }

    [Pure]
    [PublicAPI]
    public List<T> GetAllClosest((double x, double y) target, double maxDistance)
    {
        if (_elements.Count > 100 || _chunks != null) // no point with fewer because overhead is likely to exceed the individual search speed-up (unless we already chunked it up)
            return GetAllClosestChunked(target, maxDistance);

        return GetAllClosestManually(target, maxDistance);
    }


    [Pure]
    private T? GetClosestChunked((double x, double y) target)
    {
        if (_chunks == null)
            ChunkItUp();

        T? bestItem = default;
        double bestDistanceSqr = double.MaxValue;

        // We need to keep checking until we reach the furthest corner,
        // that is, we need to keep our "ring" growing span by span
        double maxPossibleDistance =
            Math.Max(
                Math.Max(
                    DistanceBetween(target, (_minX, _minY)),
                    DistanceBetween(target, (_maxX, _minY))
                ),
                Math.Max(
                    DistanceBetween(target, (_minX, _maxY)),
                    DistanceBetween(target, (_maxX, _maxY))
                )
            );
        
        int span = 0;

        do
        {
            span++;
            
            // We can check the chunks that our current "circle" covers,
            // starting at neighbouring 1 chunk away and up to all of them (and then still some until we reach corners)
            double checkDistance = _chunkSize * span;
            double checkDistanceSqr = checkDistance * checkDistance;

            IEnumerable<Chunk> chunks = GetChunksInSpan(target, span);

            foreach (Chunk chunk in chunks)
            {
                foreach ((T item, (double, double) coord) in chunk.Elements)
                {
                    double distanceSqr = DistanceBetweenSqr(
                        coord,
                        target
                    );

                    if (distanceSqr <= checkDistanceSqr) // within the "circle" covered by the span (not the chunk rectangle - we will otherwise "cut off" the items)
                    {
                        if (bestItem == null ||
                            distanceSqr < bestDistanceSqr)
                        {
                            bestDistanceSqr = distanceSqr;
                            bestItem = item;
                        }
                    }
                }
            }
            
            if (bestItem != null) // found in current "circle", so we don't need to check further
                break;

            if (checkDistance >= maxPossibleDistance)
                break;

        } while (true);

        return bestItem;
    }

    [Pure]
    private T? GetClosestChunked((double x, double y) target, double maxDistance)
    {
        if (_chunks == null)
            ChunkItUp();

        double maxDistanceSqr = maxDistance * maxDistance;
        
        IEnumerable<Chunk> chunks = GetChunksInRange(target, maxDistance);

        T? bestItem = default;
        double bestDistanceSqr = double.MaxValue;

        foreach (Chunk chunk in chunks)
        {
            foreach ((T item, (double, double) coord) in chunk.Elements)
            {
                double distanceSqr = DistanceBetweenSqr(
                    coord,
                    target
                );

                if (distanceSqr <= maxDistanceSqr)
                {
                    if (bestItem == null ||
                        distanceSqr < bestDistanceSqr)
                    {
                        bestDistanceSqr = distanceSqr;
                        bestItem = item;
                    }
                }
            }
        }

        return bestItem;
    }

    [Pure]
    private T? GetClosestManually((double x, double y) target, double? maxDistance)
    {
        double? maxDistanceSqr = maxDistance * maxDistance;

        T? bestElement = default;
        double bestDistanceSqr = 0.0;

        foreach ((T element, (double, double) coord) in _elements)
        {
            double distanceSqr = DistanceBetweenSqr(target, coord);

            if (maxDistanceSqr == null || distanceSqr <= maxDistanceSqr)
            {
                if (bestElement == null || bestDistanceSqr > distanceSqr)
                {
                    bestElement = element;
                    bestDistanceSqr = distanceSqr;
                }
            }
        }

        return bestElement;
    }

    [Pure]
    private List<T> GetAllClosestChunked((double x, double y) target, double maxDistance)
    {
        if (_chunks == null)
            ChunkItUp();
     
        double maxDistanceSqr = maxDistance * maxDistance;

        IEnumerable<Chunk> chunks = GetChunksInRange(target, maxDistance);

        SortedList<double, T> nodes = new SortedList<double, T>(DuplicateKeyComparer.Instance);
        
        foreach (Chunk chunk in chunks)
        {
            foreach ((T item, (double, double) coord) in chunk.Elements)
            {
                double distanceSqr = DistanceBetweenSqr(
                    coord,
                    target
                );

                if (distanceSqr <= maxDistanceSqr)
                {
                    nodes.Add(Math.Sqrt(distanceSqr), item);
                }
            }
        }
        
        return nodes.Values.ToList();
    }

    [Pure]
    private List<T> GetAllClosestManually((double x, double y) target)
    {
        // This is literally just sorting...
        // Hopefully, this doesn't really get called much
        
        SortedList<double, T> nodes = new SortedList<double, T>(DuplicateKeyComparer.Instance);
        
        foreach ((T item, (double, double) coord) in _elements)
        {
            double distance = DistanceBetween( // no point using Sqr version as we need the Sqrt right away anyway
                coord,
                target
            );

            nodes.Add(distance, item);
        }
        
        return nodes.Values.ToList();
    }

    [Pure]
    private List<T> GetAllClosestManually((double x, double y) target, double maxDistance)
    {
        double maxDistanceSqr = maxDistance * maxDistance;

        SortedList<double, T> nodes = new SortedList<double, T>(DuplicateKeyComparer.Instance);
        
        foreach ((T item, (double, double) coord) in _elements)
        {
            double distanceSqr = DistanceBetweenSqr(
                coord,
                target
            );

            if (distanceSqr <= maxDistanceSqr)
            {
                nodes.Add(Math.Sqrt(distanceSqr), item);
            }
        }
        
        return nodes.Values.ToList();
    }

    private void ChunkItUp()
    {
        if (_chunks != null) throw new InvalidOperationException();
        
        _chunks = new Chunk[_span, _span];

        foreach ((T element, (double x, double y) coord) in _elements)
        {
            (int chX, int chY) = GetChunkIndex(coord.x, coord.y);

            _chunks[chX, chY] ??= new Chunk();

            _chunks[chX, chY]!.Add(element, coord);
        }
    }

    [Pure]
    private IEnumerable<Chunk> GetChunksInRange((double x, double y) target, double distance)
    {
        (int chFX, int chFY) = GetChunkIndex(target.x - distance, target.y - distance);
        (int chTX, int chTY) = GetChunkIndex(target.x + distance, target.y + distance);

        for (int x = chFX; x <= chTX; x++)
            for (int y = chFY; y <= chTY; y++)
                if (_chunks![x, y] != null)
                    yield return _chunks[x, y]!;
    }

    [Pure]
    private IEnumerable<Chunk> GetChunksInSpan((double x, double y) target, int span)
    {
        (int chX, int chY) = GetChunkIndex(target.x, target.y);

        int fX = Math.Max(0, chX - span);
        int tX = Math.Min(_span - 1, chX + span);
        int fY = Math.Max(0, chY - span);
        int tY = Math.Min(_span - 1, chY + span);
        
        for (int x = fX; x <= tX; x++)
            for (int y = fY; y <= tY; y++)
                if (_chunks![x, y] != null)
                    yield return _chunks[x, y]!;
    }

    [Pure]
    private (int chX, int chY) GetChunkIndex(double x, double y)
    {
        int xc = Math.Max(0, Math.Min(_span - 1, (int)Math.Floor((x - _minX) / _size * _span)));
        int yc = Math.Max(0, Math.Min(_span - 1, (int)Math.Floor((y - _minY) / _size * _span)));
        return (xc, yc);
    }

    [Pure]
    private static double DistanceBetweenSqr((double x, double y) coord, (double x, double y) target)
    {
        double dx = coord.x - target.x;
        double dy = coord.y - target.y;
        return dx * dx + dy * dy;
    }

    [Pure]
    private static double DistanceBetween((double x, double y) coord, (double x, double y) target)
    {
        double dx = coord.x - target.x;
        double dy = coord.y - target.y;
        return Math.Sqrt(dx * dx + dy * dy);
    }


    private class Chunk
    {
        public IEnumerable<(T, (double, double))> Elements => _elements.AsEnumerable();


        private readonly List<(T, (double, double))> _elements = new List<(T, (double, double))>();


        public void Add(T element, (double, double) coord)
        {
            _elements.Add((element, coord));
        }
    }


    /// <summary>
    /// Comparer for handling equal keys when we don't care in which order they sort, just that they do.
    /// </summary>
    private class DuplicateKeyComparer : IComparer<double>
    {
        public static DuplicateKeyComparer Instance { get; } = new DuplicateKeyComparer();
        private DuplicateKeyComparer() { }
        
        public int Compare(double x, double y) => x < y ? -1 : 1;
    }
}