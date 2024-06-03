// using System;
// using System.Collections.Generic;
// using WikidataSharp;
//
// namespace Osmalyzer;
//
// [UsedImplicitly]
// public class SynchronicityWikidataData : AnalysisData
// {
//     public override string Name => "Synchronicity Wikidata";
//
//     public override string ReportWebLink => @"https://www.wikidata.org/";
//
//     public override bool NeedsPreparation => false;
//     
//
//     protected override string DataFileIdentifier => "synchronicity-wikidata";
//
//
//     public Dictionary<string, List<WikidataItem>> Items { get; private set; } = null!; // only null before prepared
//     
//
//     protected override void Download()
//     {
//         Items = new Dictionary<string, List<WikidataItem>>();
//         
//         foreach (string wikidataFieldName in WikidataSynchronicityAnalyzer.wikidataFieldNames)
//         {
//             long id = FieldNameToPropertyId(wikidataFieldName);
//             
//             Items.Add(
//                 wikidataFieldName,
//                 Wikidata.FetchItemsWithProperty(id)
//             );
//         }
//
//         return;
//
//
//         [Pure]
//         static long FieldNameToPropertyId(string name)
//         {
//             return name switch
//             {
//                 "architect:wikidata" => 84,
//
//                 _ => throw new NotImplementedException("Unknown field name: `" + name + "`, did you forget to add WD property id?.")
//             };
//         }
//     }
//
//     protected override void DoPrepare()
//     {
//         throw new InvalidOperationException();
//     }
// }