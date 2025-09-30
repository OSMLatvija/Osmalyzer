using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer;

[UsedImplicitly]
public class RestrictionRelationAnalyzer : Analyzer
{
    public override string Name => "Turn Restriction Relations";

    public override string Description => "This report checks turn restriction relations.";

    public override AnalyzerGroup Group => AnalyzerGroup.Validation;

    
    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData) ];
    

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data
        
        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmDataExtract restrictionRelations = osmData.MasterData.Filter(
            new IsRelation(),
            new HasAnyValue("type", "restriction")
        );
        
        // Parse

        List<Restriction> restrictions = [ ];

        foreach (OsmRelation osmRelation in restrictionRelations.Relations)
        {
            List<RestrictionPart> parts = [];

            foreach ((string key, string value) in osmRelation.AllTags!)
            {
                RestrictionPart? part = TryParseAsPart(key, value);
                
                if (part != null)
                    parts.Add(part);
            }
            
            restrictions.Add(new Restriction(osmRelation, parts));
        }
        
        // Stats

        report.AddGroup(
            ReportGroup.Stats, 
            "Stats"
        );
        
        report.AddEntry(
            ReportGroup.Stats, 
            new GenericReportEntry($"Found {restrictions.Count} restriction relations.")
        );

        int noTag = 0;
        int justMainTag = 0;
        int justConditionalTag = 0;
        int bothMainAndConditionalTag = 0;
        
        foreach (Restriction restriction in restrictions)
        {
            if (restriction.Parts.Count == 0)
            {
                noTag++;
            }
            else
            {
                bool hasMainTag = restriction.Parts.Any(p => p.Key == "restriction");
                bool hasConditionalTag = restriction.Parts.Any(p => p.Key == "restriction:conditional");

                if (hasMainTag && hasConditionalTag)
                    bothMainAndConditionalTag++;
                else if (hasMainTag)
                    justMainTag++;
                else if (hasConditionalTag)
                    justConditionalTag++;
            }
        }
                
        report.AddEntry(
            ReportGroup.Stats, 
            new GenericReportEntry($"{justMainTag} are with just main 'restriction' tag.")
        );
        
        report.AddEntry(
            ReportGroup.Stats, 
            new GenericReportEntry($"{justConditionalTag} are with just 'restriction:conditional' tag.")
        );
        
        report.AddEntry(
            ReportGroup.Stats, 
            new GenericReportEntry($"{bothMainAndConditionalTag} are with both 'restriction' and 'restriction:conditional' tags.")
        );

        if (noTag > 0)
        {
            report.AddEntry(
                ReportGroup.Stats,
                new GenericReportEntry($"The remaining {noTag} have no recognized restriction tags.")
            );
        }

        // TODO
        
        // no_right_turn / no_left_turn / no_u_turn / no_straight_on
        // only_right_turn / only_left_turn / only_u_turn / only_straight_on
        // no_entry, no_exit
        
        // TODO
        
        // except = psv / bicycle / hgv / motorcar / emergency
        
        // TODO
        
        // day_on / day_off / hour_on / hour_off
        
        // TODO
        
        // restriction:hgv, restriction:caravan, restriction:motorcar, restriction:bus, restriction:agricultural, restriction:motorcycle, restriction:bicycle, restriction:hazmat
    }

    
    [Pure]
    private static RestrictionPart? TryParseAsPart(string key, string value)
    {
        if (key == "restriction")
            return new RestrictionPart(key, value);
        
        if (key == "restriction:conditional")
            return new RestrictionPart(key, value);

        return null;
    }


    private record Restriction(OsmRelation Element, List<RestrictionPart> Parts);
    
    private record RestrictionPart(string Key, string Value);
    
    
    private enum ReportGroup
    {
        Stats
    }
}
