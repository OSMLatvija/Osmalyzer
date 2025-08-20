using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer;

public static class OsmGeoTools
{
    /// <summary>
    /// This is a precise calculation using the Haversine formula. 
    /// In meters.
    /// </summary>
    /// <remarks>
    /// https://stackoverflow.com/a/51839058
    /// </remarks>
    [Pure]
    public static double DistanceBetween(OsmCoord coord1, OsmCoord coord2)
    {
        double d1 = coord1.lat * (Math.PI / 180.0);
        double num1 = coord1.lon * (Math.PI / 180.0);
        double d2 = coord2.lat * (Math.PI / 180.0);
        double num2 = coord2.lon * (Math.PI / 180.0) - num1;
        double d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) + Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);

        return 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));
    }    
    
    /// <summary>
    /// Less accurate, but much faster.
    /// In meters.
    /// </summary>
    [Pure]
    public static double DistanceBetweenCheap(OsmCoord coord1, OsmCoord coord2)
    {
        double dLat = coord2.lat - coord1.lat;
        double dLon = coord2.lon - coord1.lon;

        // Approximate distance using Pythagorean theorem
        return Math.Sqrt(dLat * dLat + dLon * dLon) * 111139; // 111139 meters per degree
    }

    [Pure]
    public static OsmCoord GetAverageCoord(IEnumerable<OsmElement> elements)
    {
        return GetAverageCoord(elements.Select(e => e.AverageCoord));
    }

    [Pure]
    public static OsmCoord GetAverageCoord(IEnumerable<OsmCoord> coords)
    {
        List<OsmCoord> clone = coords.ToList();

        double averageLat = 0.0;
        double averageLon = 0.0;

        foreach (OsmCoord coord in clone)
        {
            averageLat += coord.lat / clone.Count;
            averageLon += coord.lon / clone.Count;
        }

        return new OsmCoord(averageLat, averageLon);
    }

    /// <summary>
    /// Returns the real-world area of the OSM element in km^2.
    /// </summary>
    [Pure]
    public static double GetAreaSize(OsmWay way)
    {
        if (way.Nodes.Count < 3)
            return 0.0;

        double area = 0.0;

        for (int i = 0; i < way.Nodes.Count - 1; i++)
        {
            OsmCoord coord1 = way.Nodes[i].coord;
            OsmCoord coord2 = way.Nodes[i + 1].coord;

            area += (coord2.lon - coord1.lon) * (2.0 + Math.Sin(coord1.lat * Math.PI / 180.0) + Math.Sin(coord2.lat * Math.PI / 180.0));
        }

        area = area * 6378137.0 * 6378137.0 / 2.0 / 1000000.0; // convert to km^2

        return Math.Abs(area);
    }
}