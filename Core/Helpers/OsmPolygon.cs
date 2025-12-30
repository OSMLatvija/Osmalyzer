using System;
using System.IO;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.LinearReferencing;

namespace Osmalyzer;

public class OsmPolygon
{
    private readonly List<OsmCoord> _coords = new List<OsmCoord>();


    public OsmPolygon(List<OsmCoord> coords)
    {
        _coords = coords;
    }

    public OsmPolygon(string polyFileName)
    {
        // Note: complete assumption about the file structure
            
        // none
        // 1
        //    2.659394E+01   5.566109E+01
        //    2.637334E+01   5.569487E+01
        //    ..
        //    2.659394E+01   5.566109E+01
        // END
        // END
        //
            
        string[] lines = File.ReadAllLines(polyFileName);

        for (int i = 2; i < lines.Length - 2; i++) // first and last 2 lines ignored
        {
            string[] coords = lines[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);

            double lat = double.Parse(coords[1]);
            double lon = double.Parse(coords[0]);
                
            _coords.Add(new OsmCoord(lat, lon));
        }
    }

    public bool ContainsElement(OsmElement element, RelationInclusionCheck relationInclusionCheck)
    {
        switch (element)
        {
            case OsmNode node: return ContainsCoord(node.coord);

            case OsmWay way:
            {
                OsmCoord averageCoord = way.AverageCoord;
                return ContainsCoord(averageCoord);
            }
                
            case OsmRelation relation:
            {
                switch (relationInclusionCheck)
                {
                    case RelationInclusionCheck.FuzzyLoose:
                    case RelationInclusionCheck.FuzzyStrict:
                    {
                        int count = 0;
                        int contains = 0;

                        foreach (OsmElement osmElement in relation.Elements)
                        {
                            switch (osmElement)
                            {
                                case OsmWay way:
                                {
                                    count += way.nodes.Count;
                                    if (ContainsElement(way, relationInclusionCheck))
                                        contains += way.nodes.Count;
                                    break;
                                }

                                case OsmNode node:
                                {
                                    count++;
                                    if (ContainsElement(node, relationInclusionCheck))
                                        contains++;
                                    break;
                                }
                            }
                        }

                        float threshold =
                            relationInclusionCheck == RelationInclusionCheck.FuzzyLoose ? 0.3f : 0.8f;

                        return (float)contains / count > threshold;
                    }

                    case RelationInclusionCheck.CentroidInside:
                    {
                        OsmCoord averageCoord = relation.AverageCoord;
                        return ContainsCoord(averageCoord);
                    }

                    default:
                        throw new ArgumentOutOfRangeException(nameof(relationInclusionCheck), relationInclusionCheck, null);
                }
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(element));
        }
    }

    public bool ContainsCoord(OsmCoord coord)
    {
        bool result = false;

        int b = _coords.Count - 1;

        for (int a = 0; a < _coords.Count; a++)
        {
            if (_coords[a].lon < coord.lon && _coords[b].lon >= coord.lon || _coords[b].lon < coord.lon && _coords[a].lon >= coord.lon)
                if (_coords[a].lat + (coord.lon - _coords[a].lon) / (_coords[b].lon - _coords[a].lon) * (_coords[b].lat - _coords[a].lat) < coord.lat)
                    result = !result;

            b = a;
        }

        return result;
    }

    /// <summary>
    /// Returns the coordinates of this polygon
    /// </summary>
    public List<OsmCoord> GetCoords()
    {
        return _coords;
    }

    public void SaveToFile(string fileName)
    {
        using StreamWriter streamWriter = File.CreateText(fileName);

        streamWriter.WriteLine("none");
        streamWriter.WriteLine("1");

        foreach (OsmCoord coord in _coords)
            streamWriter.WriteLine(coord.lon.ToString("E") + " " + coord.lat.ToString("E"));
            
        streamWriter.WriteLine("END");
        streamWriter.WriteLine("END");
            
        streamWriter.Close();
    }

    /// <summary>
    /// Returns the estimated overlap coverage percent between this and another polygon's boundary.
    /// The closer the two together, the higher the coverage percent.
    /// </summary>
    /// <param name="epsilon">positional uncertainty in meters, i.e. distance at which boundaries are considered the same</param>
    /// <param name="maxSamples">maximum number of sample points to check per polygon to avoid over-processing</param>
    public double GetOverlapCoveragePercent(OsmPolygon other, double epsilon = 10, int maxSamples = 300)
    {
        double coverageToOther = DirectedCoverage(this, other, epsilon, maxSamples);
        double coverageFromOther = DirectedCoverage(other, this, epsilon, maxSamples);

        return Math.Min(coverageToOther, coverageFromOther);
    }

    private static double DirectedCoverage(
        OsmPolygon source,
        OsmPolygon target,
        double epsilon,
        int maxSamples)
    {
        GeometryFactory geometryFactory = new GeometryFactory();
        
        LinearRing sourceRing = ToLinearRing(source, geometryFactory);
        LinearRing targetRing = ToLinearRing(target, geometryFactory);

        STRtree<Geometry> indexedTarget = new STRtree<Geometry>();
        indexedTarget.Insert(targetRing.EnvelopeInternal, targetRing);
        indexedTarget.Build();

        LengthIndexedLine lil = new LengthIndexedLine(sourceRing);
        double length = sourceRing.Length;

        // Convert epsilon from meters to degrees (approximate)
        // g.Distance() returns distance in degrees, so we need to convert epsilon
        double epsilonInDegrees = epsilon / 111139.0; // 111139 meters per degree
        
        // Calculate step size to limit number of samples
        // Ensure we don't exceed maxSamples
        double stepInDegrees = length / Math.Min(maxSamples, Math.Max(10, (int)(length / (epsilon / 111139.0))));

        int total = 0;
        int matched = 0;

        for (double d = 0; d <= length; d += stepInDegrees)
        {
            Coordinate c = lil.ExtractPoint(d);
            Point p = geometryFactory.CreatePoint(c);

            total++;

            foreach (Geometry? g in indexedTarget.Query(p.EnvelopeInternal))
            {
                if (g.Distance(p) <= epsilonInDegrees)
                {
                    matched++;
                    break;
                }
            }
        }

        return total == 0 ? 0.0 : (double)matched / total;
    }

    private static LinearRing ToLinearRing(OsmPolygon polygon, GeometryFactory factory)
    {
        Coordinate[] coordinates = new Coordinate[polygon._coords.Count + 1];
        
        for (int i = 0; i < polygon._coords.Count; i++)
        {
            OsmCoord coord = polygon._coords[i];
            coordinates[i] = new Coordinate(coord.lon, coord.lat);
        }
        
        coordinates[polygon._coords.Count] = coordinates[0]; // close the ring

        return factory.CreateLinearRing(coordinates);
    }


    public enum RelationInclusionCheck
    {
        /// <summary>
        /// Relations are included if a decent portion of their elements are included.
        /// This means relations like roads that barely enter the polygon won't get listed.
        /// </summary>
        FuzzyLoose,
        
        /// <summary>
        /// Relations are included if a big portion of their elements are included.
        /// This means relations like admin boundaries will get listed only if they are mostly inside.
        /// </summary>
        FuzzyStrict,
        
        CentroidInside
    }
}