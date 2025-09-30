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
            List<UnknownTag> unknownTags = [];

            foreach ((string key, string value) in osmRelation.AllTags!)
            {
                // Obviously, ignore the defining tag
                if (key == "type") continue;
                
                // Try parse as restriction part
                
                RestrictionPart? part = TryParseAsPart(key, value);

                if (part != null)
                {
                    parts.Add(part);
                    continue;
                }
                
                // Other tags that may be present, but we don't explicitly parse
                
                if (key == "note") continue;
                if (key == "fixme") continue;
                if (key == "description") continue;
                if (key == "check_date") continue;
                if (key == "source") continue;
                
                // Unknown tag
                
                unknownTags.Add(new UnknownTag(key, value));
            }
            
            // todo: parse members
            
            // todo: coord from via not average
            
            restrictions.Add(new Restriction(osmRelation, parts, unknownTags));
        }
        
        // Unknown tags
        
        report.AddGroup(
            ReportGroup.UnknownTags, 
            "Unknown Tags",
            "These relations have tags that are not known/expected keys. " +
            "These are not necessarily errors, just not recognized. " +
            "These may however be mistakes, typos or invalid tags, so they need manual checking."
        );
        
        foreach (Restriction restriction in restrictions.Where(r => r.UnknownTags.Count > 0))
        {
            report.AddEntry(
                ReportGroup.UnknownTags, 
                new IssueReportEntry(
                    $"Relation has {restriction.UnknownTags.Count} unknown tags: " +
                    string.Join(", ", restriction.UnknownTags.Select(t => $"{t.Key}={t.Value}")) + 
                    " - " + restriction.Element.OsmViewUrl,
                    restriction.Element.AverageCoord,
                    MapPointStyle.Dubious
                )
            );
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
                bool hasMainTag = restriction.Parts.Any(p => p is RestrictionPrimaryPart);
                bool hasConditionalTag = restriction.Parts.Any(p => p is RestrictionConditionalPart);

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
            return new RestrictionPrimaryPart(key, value);
        
        if (key == "restriction:conditional")
            return new RestrictionConditionalPart(key, value);

        return null;
    }


    private record Restriction(OsmRelation Element, List<RestrictionPart> Parts, List<UnknownTag> UnknownTags);
    
    private abstract record RestrictionPart(string Key, string Value);

    private record RestrictionPrimaryPart(string Key, string Value) : RestrictionPart(Key, Value);
    
    private record RestrictionConditionalPart(string Key, string Value) : RestrictionPart(Key, Value);
    
    
    private record UnknownTag(string Key, string Value);
    
    
    private enum ReportGroup
    {
        Stats,
        UnknownTags
    }
}
