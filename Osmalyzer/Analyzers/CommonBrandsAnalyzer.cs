using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class CommonBrandsAnalyzer : Analyzer
    {
        public override string Name => "Common Brands";

        public override string? Description => null;


        public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData) };
        

        public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
        {
            const int titleCountThreshold = 10;

            List<string> titleTags = new List<string>() { "brand", "name", "operator" };
            // Note that the first found is picked, so if there's no brand but is a "name", then "operator" will be ignored and "name" picked

            // Start report file

            report.WriteRawLine("These are the most common POI titles with at least " + titleCountThreshold + " occurences grouped by type (recognized by NSI):");
            
            report.WriteRawLine("title(s)" + "\t" + "count" + "\t" + "counts" + "\t" + "tag" + "\t" + "value(s)");

            // Load OSM data

            OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

            OsmMasterData osmMasterData = osmData.MasterData;
                
            OsmDataExtract titledElements = osmMasterData.Filter(
                new IsNodeOrWay(),
                new HasAnyTag(titleTags)
            );

            string nsiTagsFileName = @"data/NSI tags.tsv"; // from https://nsi.guide/?t=brands

            if (!File.Exists(nsiTagsFileName))
                nsiTagsFileName = @"../../../../" + nsiTagsFileName; // "exit" Osmalyzer\bin\Debug\net6.0\ folder and grab it from root data\
            
            string[] nsiRawTags = File.ReadAllLines(nsiTagsFileName);

            List<(string, List<string>)> nsiTags = nsiRawTags.Select(t =>
            {
                int i = t.IndexOf('\t'); 
                return (t.Substring(0, i), t.Substring(i + 1).Split(';').ToList());
            }).ToList();
            // todo: retrieve automatically from NSI repo or wherever they keep these
            // todo: would need to manually specify exceptions/grouping if parsing
            // todo: this can only group different values for the same key, not different keys

            List<(int count, string line)> reportEntries = new List<(int, string)>();
            
            foreach ((string nsiTag, List<string> nsiValues) in nsiTags)
            {
                OsmDataExtract matchingElements = titledElements.Filter(
                    new HasAnyValue(nsiTag, nsiValues)
                );

                OsmGroups titleGroupsSeparate = matchingElements.GroupByValues(titleTags, false);

                OsmMultiValueGroups titleGroupsSimilar = titleGroupsSeparate.CombineBySimilarValues(
                    (s1, s2) => string.Equals(
                        CleanName(s1), 
                        CleanName(s2), 
                        StringComparison.InvariantCulture)
                );

                string CleanName(string s)
                {
                    return s
                           .Trim()
                           .ToLower()
                           .Replace("!", "") // e.g. Top! -> Top
                           .Replace("ā", "a")
                           .Replace("č", "c")
                           .Replace("ē", "e")
                           .Replace("ģ", "g")
                           .Replace("ī", "i")
                           .Replace("ķ", "k")
                           .Replace("ļ", "l")
                           .Replace("ņ", "n")
                           .Replace("ō", "o")
                           .Replace("š", "s")
                           .Replace("ū", "u")
                           .Replace("ž", "z");
                }

                foreach (OsmMultiValueGroup group in titleGroupsSimilar.groups)
                {
                    if (group.Elements.Count >= titleCountThreshold)
                    {
                        string reportLine =
                            string.Join(", ", group.Values.Select(v => "\"" + v + "\"")) +
                            "\t" +
                            group.Elements.Count +
                            "\t" +
                            (group.Values.Count > 1 ? string.Join("+", group.ElementCounts.Select(c => c.ToString())) : "") +
                            "\t" +
                            nsiTag +
                            "\t" +
                            string.Join("; ", group.GetUniqueKeyValues(nsiTag, true)) + // just because we grouped NSI POII types, doesn't mean data has instances for each
                            "\t";

                        reportEntries.Add((group.Elements.Count, reportLine));
                    }
                }
            }

            // Each NSI tag pair has a separate name multi-value group, so we need to re-sort if we want to order by count indepenedent of POI type
            reportEntries.Sort((e1, e2) => e2.count.CompareTo(e1.count));

            foreach ((int _, string line) in reportEntries)
                report.WriteRawLine(line);

            report.WriteRawLine(
                "POI \"title\" here means the first found value from tags " + string.Join(", ", titleTags.Select(t => "\"" + t + "\"")) + ". " +
                "Title values are case-insensitive, leading/trailing whitespace ignored, Latvian diacritics ignored, character '!' ignored. " +
                "Title counts will repeat if the same element is tagged with multiple NSI POI types.");
        }
    }
}
