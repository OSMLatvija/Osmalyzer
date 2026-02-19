using System;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.LinearReferencing;
using NetTopologySuite.Simplify;

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
        // Only do one-way comparison - with simplified geometries and epsilon tolerance, this is sufficient
        return DirectedCoverage(this, other, epsilon, maxSamples);
    }

    private static double DirectedCoverage(
        OsmMultiPolygon source,
        OsmMultiPolygon target,
        double epsilon,
        int maxSamples)
    {
        GeometryFactory geometryFactory = new GeometryFactory();

        // Convert epsilon from meters to degrees (approximate)
        double epsilonInDegrees = epsilon / 111139.0;
        
        // Use epsilon as simplification tolerance - be aggressive since we're comparing with epsilon tolerance anyway
        double simplificationTolerance = epsilonInDegrees;

        // Build index of all target rings - simplify them first to reduce vertex count
        STRtree<Geometry> indexedTarget = new STRtree<Geometry>();
        
        foreach (OsmPolygon ring in target.OuterRings)
        {
            LinearRing targetRing = OsmPolygon_ToLinearRing(ring, geometryFactory);
            // Use DouglasPeucker which is faster and more aggressive than TopologyPreserving
            Geometry simplified = DouglasPeuckerSimplifier.Simplify(targetRing, simplificationTolerance);
            indexedTarget.Insert(simplified.EnvelopeInternal, simplified);
        }
        
        foreach (OsmPolygon ring in target.InnerRings)
        {
            LinearRing targetRing = OsmPolygon_ToLinearRing(ring, geometryFactory);
            Geometry simplified = DouglasPeuckerSimplifier.Simplify(targetRing, simplificationTolerance);
            indexedTarget.Insert(simplified.EnvelopeInternal, simplified);
        }
        
        indexedTarget.Build();

        double totalScore = 0.0;
        int sampleCount = 0;

        // Sample all source rings (both outer and inner) - also simplify source rings
        List<OsmPolygon> allSourceRings = new List<OsmPolygon>();
        allSourceRings.AddRange(source.OuterRings);
        allSourceRings.AddRange(source.InnerRings);

        // Reuse single Point object for all distance calculations
        Point queryPoint = geometryFactory.CreatePoint(new Coordinate(0, 0));

        foreach (OsmPolygon sourceRing in allSourceRings)
        {
            LinearRing ring = OsmPolygon_ToLinearRing(sourceRing, geometryFactory);
            
            // Simplify the source ring to reduce number of sample points needed
            Geometry simplifiedRing = DouglasPeuckerSimplifier.Simplify(ring, simplificationTolerance);
            
            LengthIndexedLine lil = new LengthIndexedLine(simplifiedRing);
            double length = simplifiedRing.Length;

            // Calculate step size to limit number of samples
            double stepInDegrees = length / Math.Min(maxSamples, Math.Max(10, (int)(length / (epsilon / 111139.0))));

            for (double d = 0; d <= length; d += stepInDegrees)
            {
                Coordinate c = lil.ExtractPoint(d);
                
                sampleCount++;

                // Create expanded envelope for spatial query
                Envelope queryEnv = new Envelope(c);
                queryEnv.ExpandBy(epsilonInDegrees);

                double minDistance = double.MaxValue;
                
                // Update the reused point's coordinate
                queryPoint.Coordinate.X = c.X;
                queryPoint.Coordinate.Y = c.Y;
                queryPoint.GeometryChanged(); // Notify geometry that coordinate changed
                
                // Query nearby geometries
                foreach (Geometry? geom in indexedTarget.Query(queryEnv))
                {
                    double distance = geom.Distance(queryPoint);
                    
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        
                        // Early exit if we found exact match
                        if (minDistance == 0)
                            break;
                    }
                }

                // Score from 1.0 (perfect match at 0 distance) to 0.0 (no match at epsilon distance)
                if (minDistance <= epsilonInDegrees)
                {
                    double score = 1.0 - (minDistance / epsilonInDegrees);
                    totalScore += score;
                }
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

    /// <summary>
    /// Checks if a coordinate is inside any outer ring and not inside any inner ring (hole)
    /// </summary>
    [Pure]
    public bool ContainsCoord(OsmCoord coord)
    {
        // Check if inside any outer ring
        bool insideOuter = false;
        foreach (OsmPolygon outerRing in OuterRings)
        {
            if (outerRing.ContainsCoord(coord))
            {
                insideOuter = true;
                break;
            }
        }

        if (!insideOuter)
            return false;

        // Check if inside any inner ring (hole)
        foreach (OsmPolygon innerRing in InnerRings)
        {
            if (innerRing.ContainsCoord(coord))
                return false;
        }

        return true;
    }
}
