using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer;

public class Chunker<T> where T : IChunkerItem
{
    private readonly List<(T, (double, double))> _elements;


    public Chunker(IList<T> items)
    {
        _elements = new List<(T, (double, double))>(items.Count);
        
        foreach (T item in items)
        {
            (double x, double y) coord = item.ChunkCoord;
            
            _elements.Add((item, coord));
        }
    }

    [Pure]
    public List<T> GetAllClosest((double lat, double lon) target, double? maxDistance)
    {
        // TODO: ACTUAL
        
        List<(double, T)> nodes = new List<(double, T)>(); // todo: presorted collection

        foreach ((T element, (double, double) coord) in _elements)
        {
            double distance = DistanceBetween(
                coord,
                target
            );

            if (maxDistance == null || distance <= maxDistance) // within max distance
            {
                nodes.Add((distance, element));
            }
        }

        return nodes.OrderBy(n => n.Item1).Select(n => n.Item2).ToList(); // todo: this is terribly slow 
    }


    [Pure]
    private double DistanceBetween((double x, double y) coord, (double x, double y) target)
    {
        double dx = coord.x - target.x;
        double dy = coord.y - target.y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}