using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer;

/// <summary>
/// Welcome to Chunks®, please choose your Chunks®.
/// </summary>
public class Chunker<T> where T : IChunkerItem
{
    // todo: move non-chunked logic here and let this handle element "limits" 
    
    public int Count => _elements.Count;

    
    private readonly List<(T, (double, double))> _elements;

    private readonly int _size; 
    
    private readonly double _minX; 
    private readonly double _minY; 
    private readonly double _maxX; 
    private readonly double _maxY; 
    
    private readonly double _width;
    private readonly double _height;
    
    private readonly double _chunkWidth;
    private readonly double _chunkHeight;

    private readonly Chunk?[,] _chunks;


    public Chunker(IList<T> items, int chunkCountPerDimension = 50)
    {
        _size = chunkCountPerDimension; 

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

        _width = _maxX - _minX;
        _height = _maxY - _minY;

        _chunkWidth = _width / _size;
        _chunkHeight = _height / _size;

        _chunks = new Chunk[_size, _size];

        foreach ((T element, (double x, double y) coord) in _elements)
        {
            (int chX, int chY) = GetChunkIndex(coord.x, coord.y);

            if (_chunks[chX, chY] == null)
            {
                (double fx, double fy, double tx, double ty) = GetChunkExtent(chX, chY);
                _chunks[chX, chY] = new Chunk(fx, fy, tx, ty);
            }

            _chunks[chX, chY]!.Add(element, coord);
        }
    }


    public T? GetClosest((double x, double y) target, double? maxDistance)
    {
        return 
            maxDistance == null ? 
                GetClosest(target) : 
                GetClosest(target, maxDistance.Value);
    }
    
    public T? GetClosest((double x, double y) target)
    {
        T? bestItem = default;
        double bestDistance = double.MaxValue;

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
            double checkDistance = _chunkWidth * span;

            IEnumerable<Chunk> chunks = GetChunksInSpan(target, span);

            foreach (Chunk chunk in chunks)
            {
                foreach ((T item, (double, double) coord) in chunk.Elements)
                {
                    double distance = DistanceBetween(
                        coord,
                        target
                    );

                    if (distance <= checkDistance) // within the "circle" covered by the span (not the chunk rectangle - we will otherwise "cut off" the items)
                    {
                        if (bestItem == null ||
                            distance < bestDistance)
                        {
                            bestDistance = distance;
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

    public T? GetClosest((double x, double y) target, double maxDistance)
    {
        IEnumerable<Chunk> chunks = GetChunksInRange(target, maxDistance);

        T? bestItem = default;
        double bestDistance = double.MaxValue;

        foreach (Chunk chunk in chunks)
        {
            foreach ((T item, (double, double) coord) in chunk.Elements)
            {
                double distance = DistanceBetween(
                    coord,
                    target
                );

                if (distance <= maxDistance) // within max distance
                {
                    if (bestItem == null ||
                        distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestItem = item;
                    }
                }
            }
        }

        return bestItem;
    }
    
    [Pure]
    public List<T> GetAllClosest((double x, double y) target, double maxDistance)
    {
        IEnumerable<Chunk> chunks = GetChunksInRange(target, maxDistance);

        List<(double, T)> nodes = new List<(double, T)>(); // todo: presorted collection
        
        foreach (Chunk chunk in chunks)
        {
            foreach ((T item, (double, double) coord) in chunk.Elements)
            {
                double distance = DistanceBetween(
                    coord,
                    target
                );

                if (distance <= maxDistance) // within max distance
                {
                    nodes.Add((distance, item));
                }
            }
        }
        
        // TODO: PROPER SORTED INSERTION
        return nodes.OrderBy(n => n.Item1).Select(n => n.Item2).ToList(); // todo: this is terribly slow 
    }

    
    [Pure]
    private IEnumerable<Chunk> GetChunksInRange((double x, double y) target, double distance)
    {
        (int chFX, int chFY) = GetChunkIndex(target.x - distance, target.y - distance);
        (int chTX, int chTY) = GetChunkIndex(target.x + distance, target.y + distance);

        for (int x = chFX; x <= chTX; x++)
            for (int y = chFY; y <= chTY; y++)
                if (_chunks[x, y] != null)
                    yield return _chunks[x, y]!;
    }

    [Pure]
    private IEnumerable<Chunk> GetChunksInSpan((double x, double y) target, int span)
    {
        (int chX, int chY) = GetChunkIndex(target.x, target.y);

        int fX = Math.Max(0, chX - span);
        int tX = Math.Min(_size - 1, chX + span);
        int fY = Math.Max(0, chY - span);
        int tY = Math.Min(_size - 1, chY + span);
        
        for (int x = fX; x <= tX; x++)
            for (int y = fY; y <= tY; y++)
                if (_chunks[x, y] != null)
                    yield return _chunks[x, y]!;
    }

    [Pure]
    private bool DoesSpanCoverAllChunks((double x, double y) target, int span)
    {
        (int chX, int chY) = GetChunkIndex(target.x, target.y);

        return
            chX - span <= 0 &&
            chY - span <= 0 &&
            chX + span >= _size - 1 &&
            chY + span >= _size - 1;
    }


    [Pure]
    private (int chX, int chY) GetChunkIndex(double x, double y)
    {
        int xc = Math.Max(0, Math.Min(_size - 1, (int)Math.Floor((x - _minX) / _width * _size)));
        int yc = Math.Max(0, Math.Min(_size - 1, (int)Math.Floor((y - _minY) / _height * _size)));
        return (xc, yc);
    }

    [Pure]
    private (double fx, double fy, double tx, double ty) GetChunkExtent(int chX, int chY)
    {
        double fx = chX == 0 ? double.MinValue : _minX + chX * _chunkWidth;
        double fy = chY == 0 ? double.MinValue : _minY + chY * _chunkHeight;
        double tx = chX == _size - 1 ? double.MaxValue : fx + _chunkWidth;
        double ty = chX == _size - 1 ? double.MaxValue : fy + _chunkHeight;
        
        return (fx, fy, tx, ty);
    }

    [Pure]
    private double DistanceBetween((double x, double y) coord, (double x, double y) target)
    {
        double dx = coord.x - target.x;
        double dy = coord.y - target.y;
        return Math.Sqrt(dx * dx + dy * dy);
        // todo: nonsqrt version
    }


    private class Chunk
    {
        public readonly double fromX;
        public readonly double fromY;
        public readonly double toX;
        public readonly double toY;
        
        
        public IEnumerable<(T, (double, double))> Elements => _elements.AsEnumerable();


        private readonly List<(T, (double, double))> _elements = new List<(T, (double, double))>();

        
        public Chunk(double fromX, double fromY, double toX, double toY)
        {
            this.fromX = fromX;
            this.fromY = fromY;
            this.toX = toX;
            this.toY = toY;
        }


        public void Add(T element, (double, double) coord)
        {
            _elements.Add((element, coord));
        }
    }
}