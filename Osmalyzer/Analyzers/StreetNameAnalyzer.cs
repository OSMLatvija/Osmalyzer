using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class StreetNameAnalyzer : Analyzer
    {
        public override string Name => "Street Names";

        public override string? Description => null;


        public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData) };
        

        public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
        {
            // Load OSM data

            OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();
           
            OsmMasterData osmMasterData = osmData.MasterData;

            OsmDataExtract osmNamedWays = osmMasterData.Filter(
                new IsWay(),
                new HasKey("name"),
                new HasAnyValue("highway", "trunk", "primary", "secondary", "tertiary", "unclassified", "residential", "living_street", "service", "track", "trunk_link", "primary_link", "secondary_link")
            );
            
            // Filter strictly to inside Latvia
            
            OsmRelation latviaRelation = (OsmRelation)osmData.MasterData.Find(
                new IsRelation(),
                new HasValue("type", "boundary"),
                new HasValue("admin_level", "2"),
                new HasValue("name", "Latvija")
            )!; // never expecting to not have this

            OsmPolygon latviaPolygon = latviaRelation.GetOuterWayPolygon();

            InsidePolygon insidePolygonFilter = new InsidePolygon(latviaPolygon, OsmPolygon.RelationInclusionCheck.Fuzzy); // somewhat expensive, so keep outside
            
            osmNamedWays = osmNamedWays.Filter(insidePolygonFilter);
            
            // Load known suffixes
            
            string knownSuffixFileName = @"data/street name suffixes.tsv";

            if (!File.Exists(knownSuffixFileName))
                knownSuffixFileName = @"../../../../" + knownSuffixFileName; // "exit" Osmalyzer\bin\Debug\net6.0\ folder and grab it from root data\
            
            string[] knownSuffixesRaw = File.ReadAllLines(knownSuffixFileName, Encoding.UTF8);

            List<KnownSuffix> knownSuffixes = knownSuffixesRaw.Select(ks => new KnownSuffix(ks)).ToList();

            // Parse

            report.AddGroup(ReportGroup.UnknownSuffixes, "Unknown street name suffixes");
            report.AddGroup(ReportGroup.KnownSuffixes, "Known street name suffixes");

            OsmGroups osmWaysByName = osmNamedWays.GroupByValues("name", false);

            foreach (OsmGroup osmGroup in osmWaysByName.groups)
            {
                string wayName = osmGroup.Value;
                
                KnownSuffix? knownSuffix = GetKnownSuffixForName(wayName, knownSuffixes);

                if (knownSuffix != null)
                {
                    knownSuffix.AddStatsFromFoundGroup(osmGroup);
                }
                else
                {
                    report.AddEntry(
                        ReportGroup.UnknownSuffixes,
                        new IssueReportEntry(
                            "Unknown suffix for \"" + wayName + "\" on " + osmGroup.Elements.Count + " road (segments)",
                            new SortEntryDesc(osmGroup.Elements.Count)
                        )
                    );
                }
            }
            
            // Known suffixes

            foreach (KnownSuffix knownSuffix in knownSuffixes)
            {
                report.AddEntry(
                    ReportGroup.KnownSuffixes,
                    new IssueReportEntry(
                        "Recognized suffix \"" + knownSuffix.Suffix + "\" found in " + knownSuffix.FoundVariantCount + " variants on " + knownSuffix.FoundTotalCount + " road (segments)",
                        new SortEntryDesc(knownSuffix.FoundVariantCount)
                    )
                );
            }


            [Pure]
            static KnownSuffix? GetKnownSuffixForName(string name, List<KnownSuffix> suffixes)
            {
                foreach (KnownSuffix knownSuffix in suffixes)
                {
                    if (name.ToLower().EndsWith(knownSuffix.Suffix)) // space not required
                    {
                        return knownSuffix;
                    }
                }

                return null;
            }
        }


        private class KnownSuffix
        {
            public string Suffix { get; }
            
            public int FoundTotalCount { get; private set; }
            
            public int FoundVariantCount { get; private set; }

            
            public KnownSuffix(string rawLine)
            {
                string[] split = rawLine.Split('\t');

                Suffix = split[0];
            }

            
            public void AddStatsFromFoundGroup(OsmGroup group)
            {
                FoundTotalCount += group.Elements.Count;
                FoundVariantCount++;
            }
        }
        
        private enum ReportGroup
        {
            UnknownSuffixes,
            KnownSuffixes
        }
    }
}