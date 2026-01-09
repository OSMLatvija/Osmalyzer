using System;
using NetTopologySuite.Geometries;

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
    /// </summary>
    /// <param name="epsilon">positional uncertainty in meters, i.e. buffer distance for boundary comparison</param>
    public double GetOverlapCoveragePercent(OsmMultiPolygon other, double epsilon = 10)
    {
        GeometryFactory geometryFactory = new GeometryFactory();

        // Convert epsilon from meters to degrees (approximate)
        double epsilonInDegrees = epsilon / 111139.0; // 111139 meters per degree

        // Convert both multipolygons to NTS geometries
        Geometry thisGeometry = ToNTSGeometry(this, geometryFactory);
        Geometry otherGeometry = ToNTSGeometry(other, geometryFactory);

        // Get boundaries (perimeters) of both geometries
        Geometry thisBoundary = thisGeometry.Boundary;
        Geometry otherBoundary = otherGeometry.Boundary;

        // Create buffers around the boundaries
        Geometry thisBuffer = thisBoundary.Buffer(epsilonInDegrees);
        Geometry otherBuffer = otherBoundary.Buffer(epsilonInDegrees);

        // Calculate the union of both buffers (total area we're comparing)
        Geometry unionBuffer = thisBuffer.Union(otherBuffer);
        double unionArea = unionBuffer.Area;

        if (unionArea == 0)
            return 0.0;

        // Calculate the symmetric difference (XOR - non-overlapping parts)
        Geometry xorBuffer = thisBuffer.SymmetricDifference(otherBuffer);
        double xorArea = xorBuffer.Area;

        // Overlap percentage = 1 - (non-overlapping area / total area)
        double overlapPercent = 1.0 - (xorArea / unionArea);

        return Math.Max(0.0, overlapPercent); // ensure non-negative
    }

    private static Geometry ToNTSGeometry(OsmMultiPolygon multiPolygon, GeometryFactory factory)
    {
        List<Polygon> polygons = new List<Polygon>();

        // Process each outer ring with its associated inner rings
        // For simplicity, we'll treat all outer rings as separate polygons without holes
        // and add inner rings as separate polygons (inverted logic)
        foreach (OsmPolygon outerRing in multiPolygon.OuterRings)
        {
            LinearRing shell = OsmPolygon_ToLinearRing(outerRing, factory);
            Polygon polygon = factory.CreatePolygon(shell);
            polygons.Add(polygon);
        }

        // Note: Inner rings represent holes, but for boundary comparison we just need their perimeters
        // So we can treat them as additional rings to compare
        foreach (OsmPolygon innerRing in multiPolygon.InnerRings)
        {
            LinearRing ring = OsmPolygon_ToLinearRing(innerRing, factory);
            Polygon polygon = factory.CreatePolygon(ring);
            polygons.Add(polygon);
        }

        if (polygons.Count == 1)
            return polygons[0];
        
        return factory.CreateMultiPolygon(polygons.ToArray());
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
