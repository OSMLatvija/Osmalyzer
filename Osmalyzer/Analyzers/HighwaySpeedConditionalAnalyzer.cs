using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class HighwaySpeedConditionalAnalyzer : Analyzer
    {
        public override string Name => "Highway Speed Conditional";

        public override string? Description => null;


        public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData) };

        
        public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
        {
            // Load OSM data

            OsmAnalysisData osmRawData = datas.OfType<OsmAnalysisData>().First();

            OsmMasterData osmMasterData = new OsmMasterData(osmRawData.DataFileName);
            
            OsmDataExtract speedLimitedRoads = osmMasterData.Filter(
                new IsWay(),
                new HasAnyValue("highway", new List<string>() { "trunk", "primary", "secondary", "tertiary", "unclassified", "residential", "service" }),
                new HasTag("maxspeed"),
                new HasTag("maxspeed:conditional")
            );
            
            // Start report file
            
            report.WriteRawLine("Ways with maxspeed and maxspeed:conditional: " + speedLimitedRoads.Elements.Count);

            // Process
            
            List<(int regular, int conditional)> limits = new List<(int regular, int conditional)>(); 
                
            foreach (OsmElement way in speedLimitedRoads.Elements)
            {
                string maxspeedStr = way.GetValue("maxspeed")!;

                if (int.TryParse(maxspeedStr, out int maxspeed))
                {
                    string maxspeedConditionalStr = way.GetValue("maxspeed:conditional")!;

                    Match match = Regex.Match(maxspeedConditionalStr, @"([0-9]+)\s*@\s*\(May 1\s*-\s*Oct 1\)");

                    if (match.Success)
                    {
                        int maxspeedConditional = int.Parse(match.Groups[1].ToString());
                        
                        if (!limits.Any(l => l.regular == maxspeed && l.conditional == maxspeedConditional))
                            limits.Add((maxspeed, maxspeedConditional));
                        
                        if (maxspeed == maxspeedConditional)
                            report.WriteRawLine("Same limits for " + maxspeed + ": " + maxspeedConditionalStr + " https://www.openstreetmap.org/way/" + way.Id);
                    }
                    else
                    {
                        if (!Regex.IsMatch(maxspeedConditionalStr, @"\d+ @ \((\w\w-\w\w )?\d\d:\d\d-\d\d:\d\d\)")) // "30 @ (Mo-Fr 07:00-19:00)" / "90 @ (22:00-07:00)"
                        {
                            report.WriteRawLine("Conditional not recognized: " + maxspeedConditionalStr + " https://www.openstreetmap.org/way/" + way.Id);
                        }
                    }
                }
                else
                {
                    report.WriteRawLine("Max speed not recognized as seasonal: " + maxspeedStr);
                }
            }

            limits.Sort();
            
            report.WriteRawLine("Combos found:");

            foreach ((int regular, int conditional) in limits)
            {
                report.WriteRawLine("Conditional limit " + conditional + " for regular limit " + regular);
            }
        }
    }
}