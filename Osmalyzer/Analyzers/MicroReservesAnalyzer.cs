using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class MicroReservesAnalyzer : Analyzer
    {
        public override string Name => "Micro Reserves";

        public override string? Description => null;


        public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData), typeof(MicroReserveAnalysisData) };
        

        public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
        {
            // Load reserve data

            List<Reserve> reserves = new List<Reserve>();
            
            MicroReserveAnalysisData reserveData = datas.OfType<MicroReserveAnalysisData>().First();

            string projectionfilePath = reserveData.ExtractionFolder + "/GIS_OZOLS_Microreserves_PUB.prj";
            CoordinateSystem ourWkt = new CoordinateSystemFactory().CreateFromWkt(File.ReadAllText(projectionfilePath));
            
            GeographicCoordinateSystem wgs84 = GeographicCoordinateSystem.WGS84;
            
            ICoordinateTransformation coordTransformation = new CoordinateTransformationFactory().CreateFromCoordinateSystems(
                ourWkt, 
                wgs84
            );

            string shapefilePath = reserveData.ExtractionFolder + "/GIS_OZOLS_Microreserves_PUB.shp";

            using ShapefileDataReader shapefileReader = new ShapefileDataReader(shapefilePath, GeometryFactory.Default);

            DbaseFileHeader dbaseHeader = shapefileReader.DbaseHeader;

            // Dump header info
            
            StreamWriter dumpFileWriter = File.CreateText("microreserves_dump.tsv");
            
            dumpFileWriter.WriteLine("Rows: Field name (shapefile); Field type (shapefile); Field name (XML); Field label (XML); Shapefile rows...");

            dumpFileWriter.WriteLine(string.Join("\t", dbaseHeader.Fields.Select(f => f.Name)));
            
            dumpFileWriter.WriteLine(string.Join("\t", dbaseHeader.Fields.Select(f => f.Type.Name)));
            
            string xmlPath = reserveData.ExtractionFolder + "/GIS_OZOLS_Microreserves_PUB.shp.xml";
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

            // Read shapes
            
            while (shapefileReader.Read())
            {
                Geometry geometry = shapefileReader.Geometry;

                // Dump all non-shape fields
                
                object?[] values = new object?[dbaseHeader.Fields.Length];
                
                for (int i = 0; i < dbaseHeader.Fields.Length; i++)
                    values[i] = shapefileReader.GetValue(i + 1); // 0 is apparently the shape itself and columns start with 1 - this doesn't exactly match XML 

                dumpFileWriter.WriteLine(string.Join("\t", values.Select(v => v != null ? v.ToString() : "NULL")));

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
            
            dumpFileWriter.Close();

            // Load OSM data

            OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();
           
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

            report.AddGroup(ReportGroup.Issues, "Unmatched Micro Reserves");
            
            report.AddEntry(
                ReportGroup.Issues,
                new PlaceholderReportEntry(
                    "All defined reserves have a matching OSM element."
                )
            );
            
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
                    
                    report.AddEntry(ReportGroup.Matched, new MapPointReportEntry(reserve.Coord, "Match!"));

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
                            reserve.Coord
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
                            osmReserve.GetAverageCoord()
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
}