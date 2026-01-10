// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text.RegularExpressions;
// using WikidataSharp;
//
// namespace Osmalyzer;
//
// [UsedImplicitly]
// public class WikidataSynchronicityAnalyzer : Analyzer
// {
//     public static readonly string[] wikidataFieldNames = {
//         "architect:wikidata"
//     };
//     // todo: automatically collect all from https://www.wikidata.org/wiki/Property:P1282 "OpenStreetMap tag or key" that are wikidata: ?
//
//
//     public override string Name => "Wikidata Synchronicity";
//
//     public override string Description => "This report checks how well auxiliary values are synchronized between OSM and Wikidata (not the wikidata themselves, but additional values).";
//
//     public override AnalyzerGroup Group => AnalyzerGroups.Misc;
//
//     public override List<Type> GetRequiredDataTypes() => new List<Type>()
//     {
//         typeof(OsmAnalysisData),
//         typeof(SynchronicityWikidataData)
//     };
//
//
//     public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
//     {
//         // Load OSM data
//
//         OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();
//
//         OsmData OsmData = osmData.MasterData;
//
//         OsmData osmWithWikidata = OsmData.Filter(
//             new HasAnyKey(
//                 wikidataFieldNames
//             )
//         );
//         
//         // Wikidata
//
//         SynchronicityWikidataData wikidataData = datas.OfType<SynchronicityWikidataData>().First();
//         
//         // Parse
//         
//         // Prepare groups
//
//         foreach (string wikidataFieldName in wikidataFieldNames)
//         {
//             report.AddGroup(wikidataFieldName, "`" + wikidataFieldName + "`");
//             // For example, `architect:wikidata`
//
//             // report.AddEntry(
//             //     wikidataFieldName,
//             //     new DescriptionReportEntry(
//             //         "Checking `" + wikidataFieldName + "`."
//             //     )
//             // );
//
//             // OSM relevant
//             
//             OsmData osmElements = osmWithWikidata.Filter(new HasKey(wikidataFieldName));
//             
//             // Wikidata relevant
//             
//             List<WikidataItem> wikidataItems = wikidataData.Items[wikidataFieldName];
//             
//             // Info
//             
//             report.AddEntry(
//                 wikidataFieldName,
//                 new GenericReportEntry(
//                     "There are " + osmElements.Elements.Count + " OSM elements with `" + wikidataFieldName + "` potentially matching " + wikidataItems.Count + " Wikidata items."
//                 )
//             );
//             
//             // Correlate
//             
//             foreach (OsmElement osmElement in osmElements.Elements)
//             {
//                 List<long> wikidataIds = GetWikidataIDsFrom(osmElement, "wikidata"); // main field, not the one we are checking
//                 // For example, `Q21734;Q49785`
//
//                 foreach (long id in wikidataIds)
//                 {
//                     WikidataItem? wikidataItem = wikidataItems.FirstOrDefault(wi => wi.ID == id);
//                     
//                     if (wikidataItem != null)
//                     {
//                         report.AddEntry(
//                             wikidataFieldName,
//                             new MapPointReportEntry(
//                                 osmElement.GetAverageCoord(),
//                                 "Found `" + wikidataFieldName + "` " +
//                                 "on OSM element " + OsmKnowledge.GetFeatureLabel(osmElement, "element", false) + osmElement.OsmViewUrl + " " +
//                                 "for Wikidata item " + wikidataItem.WikidataUrl,
//                                 MapPointStyle.Okay
//                             )
//                         );
//                     }
//                     else
//                     {
//                         report.AddEntry(
//                             wikidataFieldName,
//                             new MapPointReportEntry(
//                                 osmElement.GetAverageCoord(),
//                                 "Did not find Wikidata item `Q" + id + "` for `" + wikidataFieldName + "` " +
//                                 "on OSM " + OsmKnowledge.GetFeatureLabel(osmElement, "element", false) + " " + osmElement.OsmViewUrl,
//                                 MapPointStyle.Problem
//                             )
//                         );
//                     }
//                 }
//             }
//         }
//     }
//     
//
//     [Pure]
//     private static List<long> GetWikidataIDsFrom(OsmElement osmElement, string wikidataFieldName)
//     {
//         List<long> ids = new List<long>();
//         
//         string? osmWikidataIDString = osmElement.GetValue(wikidataFieldName);
//         
//         if (osmWikidataIDString == null)
//             return ids; // empty
//
//         string[] splits = osmWikidataIDString.Split(";");
//
//         foreach (string split in splits)
//         {
//             Match match = Regex.Match(split, @"^\s*Q([0-9]+)\s*$");
//             
//             if (match.Success)
//             {
//                 long id = long.Parse(match.Groups[1].Value);
//                 ids.Add(id);
//             }
//         }
//         
//         return ids;
//     }
// }