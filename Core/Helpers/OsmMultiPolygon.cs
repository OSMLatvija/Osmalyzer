using System;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.LinearReferencing;

namespace Osmalyzer;

/// <summary>
/// Represents a multipolygon with one or more outer rings and zero or more inner rings (holes)
/// </summary>
public class OsmMultiPolygon
{
    public List<OsmPolygon> OuterRings { get; }
    
    public List<OsmPolygon> InnerRings { get; }


    public OsmMultiPolygon(List<OsmPolygon> outerRings, List<OsmPolygon> innerRings)
    {
        OuterRings = outerRings;
        InnerRings = innerRings;
    }

    /// <summary>
    /// Creates a simple multipolygon from a single outer ring with no inner rings
    /// </summary>
    public OsmMultiPolygon(OsmPolygon outerRing)
    {
        OuterRings = [ outerRing ];
        InnerRings = [ ];
    }

    /// <summary>
    /// Creates a multipolygon from NTS Geometry, handling Polygon and MultiPolygon types
    /// </summary>
    public static OsmMultiPolygon FromNTSGeometry(Geometry geometry)
    {
        return FromNTSGeometry(geometry, null);
    }

    /// <summary>
    /// Creates a multipolygon from NTS Geometry with coordinate transformation, handling Polygon and MultiPolygon types
    /// </summary>
    /// <param name="geometry">The NTS geometry to convert</param>
    /// <param name="coordinateTransform">Optional function to transform coordinates (e.g., from local projection to WGS84). Takes (x, y) and returns (lon, lat)</param>
    public static OsmMultiPolygon FromNTSGeometry(Geometry geometry, Func<double, double, (double lon, double lat)>? coordinateTransform)
    {
        List<OsmPolygon> outerRings = [ ];
        List<OsmPolygon> innerRings = [ ];

        if (geometry is Polygon polygon)
        {
            // Single polygon
            LinearRing shell = polygon.Shell;
            outerRings.Add(CoordinateArrayToOsmPolygon(shell.Coordinates, coordinateTransform));

            // Process holes
            for (int i = 0; i < polygon.NumInteriorRings; i++)
            {
                LineString hole = polygon.GetInteriorRingN(i);
                innerRings.Add(CoordinateArrayToOsmPolygon(hole.Coordinates, coordinateTransform));
            }
        }
        else if (geometry is MultiPolygon multiPolygon)
        {
            // Multiple polygons
            for (int i = 0; i < multiPolygon.NumGeometries; i++)
            {
                Polygon poly = (Polygon)multiPolygon.GetGeometryN(i);
                
                LinearRing shell = poly.Shell;
                outerRings.Add(CoordinateArrayToOsmPolygon(shell.Coordinates, coordinateTransform));

                // Process holes in this polygon
                for (int j = 0; j < poly.NumInteriorRings; j++)
                {
                    LineString hole = poly.GetInteriorRingN(j);
                    innerRings.Add(CoordinateArrayToOsmPolygon(hole.Coordinates, coordinateTransform));
                }
            }
        }
        else
        {
            throw new ArgumentException("Geometry must be a Polygon or MultiPolygon", nameof(geometry));
        }

        return new OsmMultiPolygon(outerRings, innerRings);
    }

    private static OsmPolygon CoordinateArrayToOsmPolygon(Coordinate[] coordinates, Func<double, double, (double lon, double lat)>? coordinateTransform)
    {
        List<OsmCoord> coords = new List<OsmCoord>();
        
        // Note: NTS coordinates have lon=X and lat=Y
        foreach (Coordinate coord in coordinates)
        {
            double lat;
            double lon;
            
            if (coordinateTransform != null)
            {
                (lon, lat) = coordinateTransform(coord.X, coord.Y);
            }
            else
            {
                lat = coord.Y;
                lon = coord.X;
            }
            
            coords.Add(new OsmCoord(lat, lon));
        }

        return new OsmPolygon(coords);
    }

    /// <summary>
    /// Returns the estimated overlap coverage percent between this and another multipolygon's boundaries.
    /// The closer the two together, the higher the coverage percent.
    /// Uses gradual scoring: distance 0 = 100% match, distance epsilon = 0% match, with linear interpolation between.
    /// </summary>
    /// <param name="epsilon">positional uncertainty in meters, i.e. distance at which boundaries are considered the same</param>
    /// <param name="maxSamples">maximum number of sample points to check per ring to avoid over-processing</param>
    public double GetOverlapCoveragePercent(OsmMultiPolygon other, double epsilon = 10, int maxSamples = 300)
    {
        double coverageToOther = DirectedCoverage(this, other, epsilon, maxSamples);
        double coverageFromOther = DirectedCoverage(other, this, epsilon, maxSamples);

        return Math.Min(coverageToOther, coverageFromOther);
    }

    private static double DirectedCoverage(
        OsmMultiPolygon source,
        OsmMultiPolygon target,
        double epsilon,
        int maxSamples)
    {
        GeometryFactory geometryFactory = new GeometryFactory();

        // Build index of all target rings (both outer and inner)
        STRtree<Geometry> indexedTarget = new STRtree<Geometry>();
        
        foreach (OsmPolygon ring in target.OuterRings)
        {
            LinearRing targetRing = OsmPolygon_ToLinearRing(ring, geometryFactory);
            indexedTarget.Insert(targetRing.EnvelopeInternal, targetRing);
        }
        
        foreach (OsmPolygon ring in target.InnerRings)
        {
            LinearRing targetRing = OsmPolygon_ToLinearRing(ring, geometryFactory);
            indexedTarget.Insert(targetRing.EnvelopeInternal, targetRing);
        }
        
        indexedTarget.Build();

        // Convert epsilon from meters to degrees (approximate)
        double epsilonInDegrees = epsilon / 111139.0; // 111139 meters per degree

        double totalScore = 0.0;
        int sampleCount = 0;

        // Sample all source rings (both outer and inner)
        List<OsmPolygon> allSourceRings = new List<OsmPolygon>();
        allSourceRings.AddRange(source.OuterRings);
        allSourceRings.AddRange(source.InnerRings);

        foreach (OsmPolygon sourceRing in allSourceRings)
        {
            LinearRing ring = OsmPolygon_ToLinearRing(sourceRing, geometryFactory);
            LengthIndexedLine lil = new LengthIndexedLine(ring);
            double length = ring.Length;

            // Calculate step size to limit number of samples
            double stepInDegrees = length / Math.Min(maxSamples, Math.Max(10, (int)(length / (epsilon / 111139.0))));

            for (double d = 0; d <= length; d += stepInDegrees)
            {
                Coordinate c = lil.ExtractPoint(d);
                Point p = geometryFactory.CreatePoint(c);

                sampleCount++;

                double minDistance = double.MaxValue;
                
                foreach (Geometry? g in indexedTarget.Query(p.EnvelopeInternal))
                {
                    double distance = g.Distance(p);
                    minDistance = Math.Min(minDistance, distance);
                }

                // Score from 1.0 (perfect match at 0 distance) to 0.0 (no match at epsilon distance)
                // Linear interpolation: score = 1 - (distance / epsilon)
                if (minDistance <= epsilonInDegrees)
                {
                    double score = 1.0 - (minDistance / epsilonInDegrees);
                    totalScore += score;
                }
                // else: beyond epsilon, score is 0, so don't add anything
            }
        }

        return sampleCount == 0 ? 0.0 : totalScore / sampleCount;
    }

    private static LinearRing OsmPolygon_ToLinearRing(OsmPolygon polygon, GeometryFactory factory)
    {
        List<OsmCoord> coords = polygon.GetCoords();
        
        Coordinate[] coordinates = new Coordinate[coords.Count + 1];
        
        for (int i = 0; i < coords.Count; i++)
        {
            OsmCoord coord = coords[i];
            coordinates[i] = new Coordinate(coord.lon, coord.lat);
        }
        
        coordinates[coords.Count] = coordinates[0]; // close the ring

        return factory.CreateLinearRing(coordinates);
    }
}
