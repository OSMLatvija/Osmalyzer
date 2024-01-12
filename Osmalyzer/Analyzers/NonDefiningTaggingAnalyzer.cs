using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class NonDefiningTaggingAnalyzer : Analyzer
{
    public override string Name => "Non-defining Tagging";

    public override string Description => "This report lists features that only have tags that do not strongly define the feature.";

    public override AnalyzerGroup Group => AnalyzerGroups.Misc;


    public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData) };
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

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
                "good"      => MatchStrength.MatchedAsGood,
                "poor"      => MatchStrength.MatchedAsPoor,
                "editorial" => MatchStrength.MatchedAsEditorial,
                _           => throw new NotImplementedException()
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
                "This data is not necessarilly incorrect as such, but possibly only incorrectly tagged for OSM."
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
                "These should likely be done better, but are commonly used like that."
            )
        );
        
        // Parse

        foreach (OsmElement element in osmElements.Elements)
        {
            if (element.HasAnyTags)
            {
                bool matchAsBad = false;
                bool matchedAsPoor = false;
                bool matchedAsEditorial = false;
                
                foreach (string key in element.AllKeys!)
                {
                    foreach (DefiningKey definingKey in definingKeys)
                    {
                        MatchStrength matchStrength = Match(definingKey, key, element);

                        if (matchStrength != MatchStrength.NotMatched)
                        {
                            if (matchStrength == MatchStrength.MatchedAsGood)
                                matchAsBad = true;
                            else if (matchStrength == MatchStrength.MatchedAsPoor)
                                matchedAsPoor = true;
                            else if (matchStrength == MatchStrength.MatchedAsEditorial)
                                matchedAsEditorial = true;

                            break;
                        }
                    }

                    if (matchAsBad)
                        break;
                }
                
                [Pure]
                static MatchStrength Match(DefiningKey definingKey, string key, OsmElement element)
                {
                    switch (element)
                    {
                        case OsmNode:     if (!definingKey.Targets.HasFlag(MatchTargets.Nodes))     return MatchStrength.NotMatched; break;
                        case OsmRelation: if (!definingKey.Targets.HasFlag(MatchTargets.Relations)) return MatchStrength.NotMatched; break;
                        case OsmWay:      if (!definingKey.Targets.HasFlag(MatchTargets.Ways))      return MatchStrength.NotMatched; break;
                    }
                    
                    bool isMatch = definingKey.Method switch
                    {
                        MatchMethod.Exact  => key == definingKey.Key,
                        MatchMethod.Prefix => key.Length > definingKey.Key.Length && key.StartsWith(definingKey.Key),
                        MatchMethod.Suffix => key.Length > definingKey.Key.Length && key.EndsWith(definingKey.Key),
                        _                  => throw new NotImplementedException()
                    };

                    if (!isMatch)
                        return MatchStrength.NotMatched;

                    return definingKey.Strength;
                }

                if (!matchAsBad && !matchedAsPoor && matchedAsEditorial) // defining key not found, but it's something needed/used for OSM internal mapping
                    continue;

                if (!matchAsBad && matchedAsPoor) // defining key not found, however commonly poorly tagged key found
                {
                    if (!latviaPolygon.ContainsElement(element, OsmPolygon.RelationInclusionCheck.Fuzzy)) // expensive check, so only doing it before potentially reporting
                        continue;
                    
                    report.AddEntry(
                        ReportGroup.PoorlyDefining,
                        new MapPointReportEntry( // there are a ton of these, don't care to write a line for each
                            element.GetAverageCoord(),
                            "Element only has poorly-defining keys " + string.Join(", ", element.AllKeys!.Select(k => "`" + k + "`")) + " for " + element.OsmViewUrl, 
                            MapPointStyle.Dubious
                        )
                    );

                    // todo: summary for each unique combo
                    
                    continue;
                }

                if (matchAsBad) // defining key found
                    continue;
                
                if (!latviaPolygon.ContainsElement(element, OsmPolygon.RelationInclusionCheck.Fuzzy)) // expensive check, so only doing it before potentially reporting
                    continue;

                report.AddEntry(
                    ReportGroup.NonDefining,
                    new IssueReportEntry(
                        "Element only has non-defining keys " + string.Join(", ", element.AllKeys!.Select(k => "`" + k + "`")) + " for " + element.OsmViewUrl,
                        element.GetAverageCoord(), 
                        MapPointStyle.Problem
                    )
                );
            }
        }
    }

    private record DefiningKey(string Key, MatchStrength Strength, MatchMethod Method, MatchTargets Targets);

    private enum MatchStrength
    {
        MatchedAsGood,
        NotMatched,
        MatchedAsPoor,
        MatchedAsEditorial
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