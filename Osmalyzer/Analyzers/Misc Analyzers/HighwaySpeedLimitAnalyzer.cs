using System;
using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer;

[UsedImplicitly]
public class HighwaySpeedLimitAnalyzer : Analyzer
{
    public override string Name => "Highway Speed Limits";

    public override string Description => "This report checks that various speed limits are correct. This doesn't check living zones, see separate report.";

    public override AnalyzerGroup Group => AnalyzerGroups.Road;

    public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(LatviaOsmAnalysisData) };
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract osmRoads = osmMasterData.Filter(
            new IsWay(),
            new HasAnyValue("maxspeed", "80", "90"),
            new HasAnyValue("highway", "trunk", "primary", "secondary", "tertiary", "unclassified", "residential", "trunk_link", "primary_link", "secondary_link"),
            new HasKey("surface"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.Fuzzy)
        );
            
        // Parse

        report.AddGroup(ReportGroup.Unpaved90, 
                        "Unpaved roads with 90", 
                        "These roads are unpaved and have `maxspeed=90`. Unpaved roads in Latvia are limited to 80 by default. It's most likely a mistake, usually because `surface` or `maxspeed` were set at different times and/or changed over time.",
                        "No unpaved roads have a speed limit of 90.");
            
        report.AddGroup(ReportGroup.Paved80, 
                        "Paved roads with 80", 
                        "These roads are paved but have `maxspeed=80`. It's possibly a mistake if the surface or speed limits have changed. It is however perfectly possibly for a paved road to have a signed speed limit of 80. Roads with `maxspeed:type=sign` are ignored - it's probably a good idea to tag false positives if signage can be confirmed.", 
                        "No unpaved roads have a speed limit of 80.");

        // Unpaved 90
            
        OsmDataExtract unpavedRoads90 = osmRoads.Filter(
            new HasValue("maxspeed", "90"),
            new HasAnyValue("surface", "unpaved", "ground", "gravel", "dirt", "grass", "compacted", "sand", "fine_gravel", "earth", "pebblestone"),
            new DoesntHaveAnyValue("maxspeed:type", "sign", "LV:zone90")
        );

        OsmGroups unpavedRoads90Grouped = unpavedRoads90.GroupByValues(new List<string>() { "ref", "name" }, false);

        foreach (OsmGroup group in unpavedRoads90Grouped.groups) // we only have ways
        {
            List<string> surfaces = group.CollectValues("surface");
            List<string> refs = group.CollectValues("ref");
            List<string> namees = group.CollectValues("name");

            report.AddEntry(
                ReportGroup.Unpaved90,
                new IssueReportEntry(
                    (group.Count > 1 ? "These " : "This") + " " +
                    (refs.Count == 1 ? "`" + refs[0] + "` " : "") +
                    (namees.Count == 1 ? "`" + namees[0] + "` " : "") +
                    (group.Count > 1 ? "road segments have" : "road segment has") + " " +
                    "`maxspeed=90`, but unpaved " +
                    (surfaces.Count > 1 ? "`surface`s " + string.Join(", ", surfaces.Select(s => "`" + s + "`")) : "`surface=" + surfaces[0] + "`") +
                    " " + ReportEntryFormattingHelper.ListElements(group.Elements, 100),
                    group.GetAverageElementCoord(),
                    MapPointStyle.Problem
                )
            );
        }
            
        // Paved 80
            
        OsmDataExtract pavedRoads80 = osmRoads.Filter(
            new HasValue("maxspeed", "80"),
            new HasAnyValue("surface", "asphalt", "paved", "concrete", "chipseal"),
            new DoesntHaveAnyValue("maxspeed:type", "sign", "LV:zone80")
        );
        // Stuff that's not expected to be high speed "paving_stones", "sett", "wood", "cobblestone", "concrete:plates", "metal", "grass_paver",
            
        OsmGroups pavedRoads80Grouped = pavedRoads80.GroupByValues(new List<string>() { "ref", "name" }, false);

        foreach (OsmGroup group in pavedRoads80Grouped.groups) // we only have ways
        {
            List<string> surfaces = group.CollectValues("surface");
            List<string> refs = group.CollectValues("ref");
            List<string> namees = group.CollectValues("name");
                
            // TODO: ? zone:maxspeed=LV:80 / source:maxspeed=LV:zone80

            report.AddEntry(
                ReportGroup.Paved80,
                new IssueReportEntry(
                    (group.Count > 1 ? "These " : "This") + " " +
                    (refs.Count == 1 ? "`" + refs[0] + "` " : "") +
                    (namees.Count == 1 ? "`" + namees[0] + "` " : "") +
                    (group.Count > 1 ? "road segments have" : "road segment has") + " " +
                    "`maxspeed=80`, but paved " +
                    (surfaces.Count > 1 ? "`surface`s " + string.Join(", ", surfaces.Select(s => "`" + s + "`")) : "`surface=" + surfaces[0] + "`") +
                    " " + ReportEntryFormattingHelper.ListElements(group.Elements, 100),
                    group.GetAverageElementCoord(),
                    MapPointStyle.Problem
                )
            );
        }
    }
        
        
    private enum ReportGroup
    {
        Unpaved90,
        Paved80
    }
}