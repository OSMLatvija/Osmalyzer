using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Osmalyzer
{
    public class HighwaySpeedConditionalAnalyzer : Analyzer
    {
        public override string Name => "Highway Speed Conditional";


        public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData) };

        
        public override void Run(IEnumerable<AnalysisData> datas)
        {
            // Load OSM data

            OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();
            
            OsmBlob speedLimitedRoads = new OsmBlob(
                osmData.DataFileName,
                new IsWay(),
                new HasAnyValue("highway", new List<string>() { "trunk", "primary", "secondary", "tertiary", "unclassified", "residential", "service" }),
                new HasTag("maxspeed"),
                new HasTag("maxspeed:conditional")
            );
            
            // Start report file
            
            const string reportFileName = @"Max speed conditional report.txt";
            
            using StreamWriter reportFile = File.CreateText(reportFileName);

            reportFile.WriteLine("Ways with maxspeed and maxspeed:conditional: " + speedLimitedRoads.Elements.Count);

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
                            reportFile.WriteLine("Same limits for " + maxspeed + ": " + maxspeedConditionalStr + " https://www.openstreetmap.org/way/" + way.Id);
                    }
                    else
                    {
                        if (!Regex.IsMatch(maxspeedConditionalStr, @"\d+ @ \((\w\w-\w\w )?\d\d:\d\d-\d\d:\d\d\)")) // "30 @ (Mo-Fr 07:00-19:00)" / "90 @ (22:00-07:00)"
                        {
                            reportFile.WriteLine("Conditional not recognized: " + maxspeedConditionalStr + " https://www.openstreetmap.org/way/" + way.Id);
                        }
                    }
                }
                else
                {
                    reportFile.WriteLine("Max speed not recognized as seasonal: " + maxspeedStr);
                }
            }

            limits.Sort();
            
            reportFile.WriteLine("Combos found:");

            foreach ((int regular, int conditional) in limits)
            {
                reportFile.WriteLine("Conditional limit " + conditional + " for regular limit " + regular);
            }
                
            
            // Finish report file
                
            reportFile.WriteLine("Data as of " + osmData.DataDate + ". Provided as is; mistakes possible.");

            reportFile.Close();

#if !REMOTE_EXECUTION
            // Launch the text file in default reader (Notepad or smt)
            Process.Start(new ProcessStartInfo(reportFileName)
            {
                Verb = "open",
                UseShellExecute = true
            });
#endif    
        }
    }
}