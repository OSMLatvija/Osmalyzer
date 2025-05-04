using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer;

public static class OsmGeoTools
{
    /// <summary>
    /// In meters.
    /// https://stackoverflow.com/a/51839058
    /// </summary>
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

    [Pure]
    public static OsmCoord GetAverageCoord(IEnumerable<OsmElement> elements)
    {
        return GetAverageCoord(elements.Select(e => e.GetAverageCoord()));
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
}