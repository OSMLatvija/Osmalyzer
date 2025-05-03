using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace Osmalyzer;

[UsedImplicitly]
public class MicroReservesAnalyzer : Analyzer
{
    public override string Name => "Micro Reserves";

    public override string Description => "This report checks that excepted microreserves are mapped";

    public override AnalyzerGroup Group => AnalyzerGroups.Misc;

    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData), typeof(MicroReserveAnalysisData) ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load reserve data

        List<Reserve> reserves = new List<Reserve>();
            
        MicroReserveAnalysisData reserveData = datas.OfType<MicroReserveAnalysisData>().First();

        string projectionfilePath = Path.Combine(Path.GetFullPath(reserveData.ExtractionFolder), "GIS_OZOLS_Microreserves_PUB.prj");
        CoordinateSystem ourWkt = new CoordinateSystemFactory().CreateFromWkt(File.ReadAllText(projectionfilePath));
            
        GeographicCoordinateSystem wgs84 = GeographicCoordinateSystem.WGS84;
            
        ICoordinateTransformation coordTransformation = new CoordinateTransformationFactory().CreateFromCoordinateSystems(
            ourWkt, 
            wgs84
        );

        string shapefilePath = Path.Combine(Path.GetFullPath(reserveData.ExtractionFolder), "GIS_OZOLS_Microreserves_PUB.shp");

        using ShapefileDataReader shapefileReader = new ShapefileDataReader(shapefilePath, GeometryFactory.Default);

        DbaseFileHeader dbaseHeader = shapefileReader.DbaseHeader;

#if !REMOTE_EXECUTION
        // Dump header info
            
        StreamWriter dumpFileWriter = File.CreateText("microreserves_dump.tsv");
            
        dumpFileWriter.WriteLine("Rows: Field name (shapefile); Field type (shapefile); Field name (XML); Field label (XML); Shapefile rows...");

        dumpFileWriter.WriteLine(string.Join("\t", dbaseHeader.Fields.Select(f => f.Name)));
            
        dumpFileWriter.WriteLine(string.Join("\t", dbaseHeader.Fields.Select(f => f.Type.Name)));
            
        string xmlPath = Path.Combine(Path.GetFullPath(reserveData.ExtractionFolder), "GIS_OZOLS_Microreserves_PUB.shp.xml");
        MatchCollection fieldDescMatches = Regex.Matches(File.ReadAllText(xmlPath), @"<attrlabl Sync=""TRUE"">([^<]+)</attrlabl><attalias Sync=""TRUE"">([^<]+)</attalias>");

        dumpFileWriter.WriteLine(string.Join("\t", dbaseHeader.Fields.Select(f =>
        {
            Match? match = fieldDescMatches.FirstOrDefault(m => m.Groups[1].ToString().StartsWith(f.Name));
            return match != null ? match.Groups[1].ToString() : "UNMATCHED";
        })));
        dumpFileWriter.WriteLine(string.Join("\t", dbaseHeader.Fields.Select(f =>
        {
            Match? match = fieldDescMatches.FirstOrDefault(m => m.Groups[1].ToString().StartsWith(f.Name));
            return match != null ? match.Groups[2].ToString() : "UNMATCHED";
        })));
        // Note the match check and StartsWith because the shapefile data header names are both wrong and mismatched
        // The reader doesn't have this meta-info or at least I didn't find it, so I just manually grab it from XML
#endif

        // Read shapes
            
        while (shapefileReader.Read())
        {
            Geometry geometry = shapefileReader.Geometry;

#if !REMOTE_EXECUTION
            // Dump all non-shape fields
                
            object?[] values = new object?[dbaseHeader.Fields.Length];
                
            for (int i = 0; i < dbaseHeader.Fields.Length; i++)
                values[i] = shapefileReader.GetValue(i + 1); // 0 is apparently the shape itself and columns start with 1 - this doesn't exactly match XML 

            int mrObject = (int)(double)values[1]!;
            int mrType = (int)(double)values[8]!;
                
            dumpFileWriter.WriteLine(
                string.Join("\t", values.Select(v => v != null ? v.ToString() : "NULL")) +
                "\t" + MakeProtectionObjectValue(mrObject, mrType)
            );
                
            [Pure]
            static string MakeProtectionObjectValue(int mrObject, int mrType)
            {
                switch (mrObject)
                {
                    case 0: // Nav definēts
                        return MakeTypeValue(mrType); 
                    // There is exactly 1 entry 0-4 - "birds" type but undefined object
                            
                    case 1: // Biotops un suga
                        if (mrType == 10)
                            return "biotope";
                                
                        return "biotope; " + MakeTypeValue(mrType);
                            
                    case 2: // Biotops
                        if (mrType != 10) throw new NotImplementedException();
                        // All entries are 2-10
                            
                        return "biotope";
                            
                    case 3: // Suga
                        return MakeTypeValue(mrType);
                        
                    default: 
                        throw new NotImplementedException();
                }

                [Pure]
                static string MakeTypeValue(int mrType)
                {
                    switch (mrType)
                    {
                        case 0:  return "amphibians"; // Abinieki
                        case 1:  return "reptiles"; // Rāpuļi
                        case 2:  return "invertebrates"; // Bezmugurkaulnieki
                        case 3:  return "fish"; // Zivis
                        case 4:  return "birds"; // Putni
                        case 5:  return "mammals"; // Zīdītāji
                        case 6:  return "vascular plants and ferns"; // Vaskulārie augi un paparžaugi
                        case 7:  return "lichens"; // Ķērpji
                        case 8:  return "moss"; // Sūnas
                        case 9:  return "mushrooms"; // Sēnes
                        case 10: return "biotope"; // Biotopi
                            
                        default: throw new NotImplementedException();
                    }
                }
            }
#endif

            // Process shape
                
            Point centroid = geometry.Centroid;

            (double lon, double lat) = coordTransformation.MathTransform.Transform(centroid.X, centroid.Y);

            OsmCoord coord = new OsmCoord(lat, lon);
                
            reserves.Add(
                new Reserve(
                    coord,
                    geometry.Area
                )
            );
        }

#if !REMOTE_EXECUTION
        dumpFileWriter.Close();
#endif

        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract osmReserves = osmMasterData.Filter(
            new OrMatch(
                new AndMatch(
                    new IsWay(),
                    new HasValue("leisure", "nature_reserve")
                ),
                new AndMatch(
                    new IsWayOrRelation(),
                    new HasValue("boundary", "protected_area")
                )
            )
        );

        // TODO: https://likumi.lv/ta/id/20083-noteikumi-par-dabas-liegumiem - full reserves
            
        // Parse

        report.AddGroup(ReportGroup.Issues, "Unmatched Micro Reserves", null, "All defined reserves have a matching OSM element.");
            
        report.AddGroup(ReportGroup.Matched, "Matched Micro Reserves");

        int matchedCount = 0;

        List<(OsmElement osm, List<Reserve> reserves)> matches = new List<(OsmElement, List<Reserve>)>(); 
            
        foreach (Reserve reserve in reserves)
        {
            const int searchDistance = 300;
                
            OsmElement? osmReserve = osmReserves.GetClosestElementTo(reserve.Coord, searchDistance, out double? closestDistance);

            if (osmReserve != null)
            {
                matchedCount++;

                if (closestDistance > 50)
                {
                    // todo: we have like 3000 unmatched, so this wouldn't help
                }
                    
                report.AddEntry(
                    ReportGroup.Matched, 
                    new MapPointReportEntry(
                        reserve.Coord, 
                        "Match!",
                        MapPointStyle.Okay
                    )
                );

                (OsmElement _, List<Reserve> previousMatchedReserves) = matches.FirstOrDefault(m => m.osm == osmReserve);
                if (previousMatchedReserves != null)
                    previousMatchedReserves.Add(reserve);
                else
                    matches.Add((osmReserve, new List<Reserve>() { reserve }));
            }
            else
            {
                report.AddEntry(
                    ReportGroup.Issues,
                    new IssueReportEntry(
                        "Couldn't find an OSM element for micro-reserve " + reserve + " within " + searchDistance + " m.",
                        reserve.Coord,
                        MapPointStyle.Problem
                    )
                );
            }
        }

        int multimatches = 0;
            
        foreach ((OsmElement osmReserve, List<Reserve> matchedReserves) in matches)
        {
            if (matchedReserves.Count > 1)
            {
                multimatches++;

                report.AddEntry(
                    ReportGroup.Issues,
                    new IssueReportEntry(
                        "OSM reserve " + osmReserve.OsmViewUrl + " " +
                        "matched to multiple reserves - " + string.Join("; ", matchedReserves.Select(r => r.ToString())) + ".",
                        osmReserve.GetAverageCoord(),
                        MapPointStyle.Dubious
                    )
                );
            }
        }

        report.AddEntry(
            ReportGroup.Issues,
            new DescriptionReportEntry(
                "Matched " + matchedCount + "/" + reserves.Count + " reserves to " + matches.Count + "/" + osmReserves.Count + " OSM elements with " + multimatches + " multi-matches."
            )
        );
    }

    private class Reserve
    {
        public OsmCoord Coord { get; }
            
        public double Area { get; }


        public Reserve(OsmCoord coord, double area)
        {
            Coord = coord;
            Area = area;
        }


        public override string ToString()
        {
            return "at " + Coord.OsmUrl + " of " + (Area / 1_000_000).ToString("F2") + " km²";
        }
    }
        
    private enum ReportGroup
    {
        Issues,
        Matched
    }
}