using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Osmalyzer;

[UsedImplicitly]
public class NonDefiningTaggingAnalyzer : Analyzer
{
    public override string Name => "Non-defining Tagging";

    public override string Description => "This report lists features that only have tags that do not strongly define the feature.";

    public override AnalyzerGroup Group => AnalyzerGroups.Misc;


    public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(LatviaOsmAnalysisData) };
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;
        
        OsmDataExtract osmElements = osmMasterData.Filter(
            new HasAnyKey()
            //new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.Fuzzy) -- this is way too slow for so many elements
        );

        OsmPolygon latviaPolygon = BoundaryHelper.GetLatviaPolygon(osmData.MasterData);

        // Load defining tags

        string definingKeysFileName = @"data/feature defining keys.tsv";

        if (!File.Exists(definingKeysFileName))
            definingKeysFileName = @"../../../../" + definingKeysFileName; // "exit" Osmalyzer\bin\Debug\net6.0\ folder and grab it from root data\
            
        string[] definingKeysRaw = File.ReadAllLines(definingKeysFileName);

        List<DefiningKey> definingKeys = definingKeysRaw.Select(dk =>
        {
            string[] split = dk.Split('\t');
            return new DefiningKey(split[0], StringToStrength(split[1]), StringToMethod(split[2]), StringToTargets(split[3]));
        }).ToList();
        
        [Pure]
        static MatchStrength StringToStrength(string str)
        {
            return str switch
            {
                "good"       => MatchStrength.MatchedAsGood,
                "poor"       => MatchStrength.MatchedAsPoor,
                "editorial"  => MatchStrength.MatchedAsEditorial,
                "strippable" => MatchStrength.Strippable,
                _            => throw new NotImplementedException()
            };
        }
        
        [Pure]
        static MatchMethod StringToMethod(string str)
        {
            return str switch
            {
                "exact"  => MatchMethod.Exact,
                "prefix" => MatchMethod.Prefix,
                "suffix" => MatchMethod.Suffix,
                _        => throw new NotImplementedException()
            };
        }
        
        [Pure]
        static MatchTargets StringToTargets(string str)
        {
            return str switch
            {
                "nwr" => MatchTargets.All,
                "n"   => MatchTargets.Nodes,
                "w"   => MatchTargets.Ways,
                "r"   => MatchTargets.Relations,
                "nw"  => MatchTargets.NodesAndWays,
                "wr"  => MatchTargets.WaysAndRelations,
                "nr"  => MatchTargets.NodesAndRelations,
                _     => throw new NotImplementedException()
            };
        }

        // Prepare groups

        report.AddGroup(ReportGroup.NonDefining, "Non-defining tagging");

        report.AddEntry(
            ReportGroup.NonDefining,
            new DescriptionReportEntry(
                "These elements/features more than likely need a proper main tag, because the tags they have were't recognized as something that defines them as a feature in OSM sense (see below for tag list). " +
                "This data is not necessarily incorrect as such, but possibly only incorrectly tagged for OSM. " +
                "As there are many obscure tags on OSM, make sure to actually check that they are actually wrong."
            )
        );

        report.AddEntry(
            ReportGroup.NonDefining,
            new GenericReportEntry(
                "These tags are considered to define a feature: " + 
                string.Join(", ", definingKeys.Where(dk => dk.Strength == MatchStrength.MatchedAsGood).Select(dk => "`" + dk.Key + "`" + DefiningKeyQualifiers(dk)))
                // todo: comment?
            )
        );

        [Pure]
        static string DefiningKeyQualifiers(DefiningKey key)
        {
            List<string> qualifiers = new List<string>();

            if (key.Targets != MatchTargets.All)
                qualifiers.Add(TargetsToString(key.Targets));
            
            if (key.Method != MatchMethod.Exact)
                qualifiers.Add(MethodToString(key.Method));

            if (qualifiers.Count == 0)
                return "";

            return " (" + string.Join(", ", qualifiers) + ")";
                
            
            [Pure]
            static string TargetsToString(MatchTargets targets)
            {
                return targets switch
                {
                    MatchTargets.Nodes             => "nodes",
                    MatchTargets.Ways              => "ways",
                    MatchTargets.Relations         => "relation",
                    MatchTargets.NodesAndWays      => "nodes/ways",
                    MatchTargets.WaysAndRelations  => "ways/relations",
                    MatchTargets.NodesAndRelations => "nodes/relations",
                    _                              => throw new NotImplementedException()
                };
            }

            [Pure]
            static string MethodToString(MatchMethod method)
            {
                return method switch
                {
                    MatchMethod.Exact  => "exact",
                    MatchMethod.Prefix => "prefix",
                    MatchMethod.Suffix => "suffix",
                    _                  => throw new NotImplementedException()
                };
            }
        }

        report.AddGroup(ReportGroup.PoorlyDefining, "Poorly-defining tagging");

        report.AddEntry(
            ReportGroup.PoorlyDefining,
            new DescriptionReportEntry(
                "These should likely be done better, but are commonly used like that. These are listed in a separate section, because there are too many elements with these keys to manually review above."
            )
        );

        report.AddEntry(
            ReportGroup.PoorlyDefining,
            new GenericReportEntry(
                "These tags are considered to poorly define a feature (although technically valid by itself): " + 
                string.Join(", ", definingKeys.Where(dk => dk.Strength == MatchStrength.MatchedAsPoor).Select(dk => "`" + dk.Key + "`" + DefiningKeyQualifiers(dk)))
                // todo: comment?
            )
        );
        
        // Parse
        
        Dictionary<MatchStrength, int> matchCounts = new Dictionary<MatchStrength, int>(); // outer for performance
        MatchStrength[] strengths = (MatchStrength[])Enum.GetValues(typeof(MatchStrength));
        foreach (MatchStrength matchStrength in strengths)
            matchCounts.Add(matchStrength, 0); // preload values for performance
        
        Dictionary<string, (int, List<string>, List<OsmElement.OsmElementType>)> totalPoorMatchCounts = new Dictionary<string, (int, List<string>, List<OsmElement.OsmElementType>)>();

        foreach (OsmElement element in osmElements.Elements)
        {
            if (element.HasAnyTags)
            {
                foreach (MatchStrength matchStrength in strengths)
                    matchCounts[matchStrength] = 0;

                List<Match> matches = new List<Match>();
                
                foreach (string key in element.AllKeys!)
                {
                    Match? match = null;

                    foreach (DefiningKey definingKey in definingKeys)
                    {
                        match = Match(definingKey, key, element);
                        if (match != null) // this will only match ocne, but we don't expect defining tags to have multiple matches
                            break;
                    }
                    
                    if (match != null)
                        matches.Add(match);
                    
                    matchCounts[match?.Strength ?? MatchStrength.NotMatched]++;
                }

                [Pure]
                static Match? Match(DefiningKey definingKey, string key, OsmElement element)
                {
                    switch (element)
                    {
                        case OsmNode:     if (!definingKey.Targets.HasFlag(MatchTargets.Nodes))     return null; break;
                        case OsmRelation: if (!definingKey.Targets.HasFlag(MatchTargets.Relations)) return null; break;
                        case OsmWay:      if (!definingKey.Targets.HasFlag(MatchTargets.Ways))      return null; break;
                    }
                    
                    bool isMatch = definingKey.Method switch
                    {
                        MatchMethod.Exact  => key == definingKey.Key,
                        MatchMethod.Prefix => key.Length > definingKey.Key.Length && key.StartsWith(definingKey.Key),
                        MatchMethod.Suffix => key.Length > definingKey.Key.Length && key.EndsWith(definingKey.Key),
                        _                  => throw new NotImplementedException()
                    };

                    if (!isMatch)
                        return null;

                    return new Match(definingKey.Strength, key, element.GetValue(key)!);
                }

                if (matchCounts[MatchStrength.MatchedAsGood] > 0)
                    continue; // strongly-matched tags, so this represents a feature - nothing to report 

                if (matchCounts[MatchStrength.MatchedAsPoor] > 0) // defining keys not found, however commonly poorly-tagged and semi-acceptable keys found
                {
                    if (!latviaPolygon.ContainsElement(element, OsmPolygon.RelationInclusionCheck.Fuzzy)) // expensive check, so only doing it before potentially reporting
                        continue;
                    
                    report.AddEntry(
                        ReportGroup.PoorlyDefining,
                        new MapPointReportEntry( // there are a ton of these, don't care to write a line for each
                            element.GetAverageCoord(),
                            "Element only has poorly-defining keys for " + element.OsmViewUrl,
                            element,
                            MapPointStyle.Dubious
                        )
                    );

                    Match poorMatch = matches.First(m => m.Strength == MatchStrength.MatchedAsPoor);

                    if (!totalPoorMatchCounts.ContainsKey(poorMatch.Key))
                    {
                        totalPoorMatchCounts.Add(poorMatch.Key, (1, new List<string>() { poorMatch.Value }, new List<OsmElement.OsmElementType>() { element.ElementType }));
                    }
                    else
                    {
                        totalPoorMatchCounts[poorMatch.Key].Item2.Add(poorMatch.Value);
                        totalPoorMatchCounts[poorMatch.Key].Item3.Add(element.ElementType);
                        totalPoorMatchCounts[poorMatch.Key] = (totalPoorMatchCounts[poorMatch.Key].Item1 + 1, totalPoorMatchCounts[poorMatch.Key].Item2, totalPoorMatchCounts[poorMatch.Key].Item3);
                    }

                    continue;
                }

                if (matchCounts[MatchStrength.NotMatched] == 0)
                    continue; // all the other tags (since there are no good or poor) are editorial (like fixme) or strippable when alone (like addr:)
                
                // At this point we have tags that were not matched
                
                if (!latviaPolygon.ContainsElement(element, OsmPolygon.RelationInclusionCheck.Fuzzy)) // expensive check, so only doing it before potentially reporting
                    continue;

                string keys = string.Join(", ", element.AllKeys!.Select(k => "`" + k + "`"));
                report.AddEntry(
                    ReportGroup.NonDefining,
                    new IssueReportEntry(
                        "Element only has non-defining keys " + keys + " for " + element.OsmViewUrl,
                        new SortEntryAsc(keys), 
                        element.GetAverageCoord(),
                        MapPointStyle.Problem
                    )
                );
                
                // todo: actually list which tag were the bad/unmatched ones
            }
        }

        foreach ((string? key, (int count, List<string>? values, List<OsmElement.OsmElementType> elementTypes)) in totalPoorMatchCounts)
        {
            report.AddEntry(
                ReportGroup.PoorlyDefining,
                new IssueReportEntry(
                    "Matched `" + key + "` key " + count + " times with " + values.Distinct().Count() + " unique values for " + string.Join(", ", elementTypes.Distinct().Select(dt => elementTypes.Count(t => t == dt) + " " + ElementTypeToString(dt)))
                )
            );

            // IEnumerable<(string, int)> valueCounts = values.Distinct().Select(dv => (dv, values.Count(v => v == dv))).OrderBy(vc => vc.Item2);
            // File.WriteAllLines("source alone values.tsv", valueCounts.Select(vc => vc.Item1 + "\t" + vc.Item2));
            
            continue;
            

            [Pure]
            static string ElementTypeToString(OsmElement.OsmElementType type)
            {
                return type switch
                {
                    OsmElement.OsmElementType.Node     => "nodes",
                    OsmElement.OsmElementType.Way      => "ways",
                    OsmElement.OsmElementType.Relation => "relations",
                };
            }
        }
    }

    private record DefiningKey(string Key, MatchStrength Strength, MatchMethod Method, MatchTargets Targets);

    private record Match(MatchStrength Strength, string Key, string Value);
    
    private enum MatchStrength
    {
        MatchedAsGood,
        NotMatched,
        MatchedAsPoor,
        MatchedAsEditorial,
        Strippable
    }

    private enum MatchMethod
    {
        Exact,
        Prefix,
        Suffix
    }

    [Flags]
    private enum MatchTargets
    {
        Nodes = 1 << 0,
        Ways = 1 << 1,
        Relations = 1 << 2,
        NodesAndWays = Nodes | Ways,
        NodesAndRelations = Nodes | Relations,
        WaysAndRelations = Ways | Relations,
        All = Nodes | Ways | Relations
    }
    
    private enum ReportGroup
    {
        NonDefining,
        PoorlyDefining
    }
}