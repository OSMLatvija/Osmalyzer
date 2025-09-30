namespace Osmalyzer;

[UsedImplicitly]
public class RestrictionRelationAnalyzer : Analyzer
{
    public override string Name => "Turn Restriction Relations";

    public override string Description => "This report checks turn restriction relations, i.e. relations with `type=restriction`.";

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
            List<RestrictionEntry> entries = [];
            List<UnknownTag> unknownTags = [];
            List<DeprecatedTag> deprecatedTags = [];
            RestrictionExceptions? exception = null;

            foreach ((string key, string value) in osmRelation.AllTags!)
            {
                // Obviously, ignore the defining tag
                if (key == "type") continue;
                
                // Try parse as restriction entry
                
                RestrictionEntry? entry = TryParseAsEntry(key, value);

                if (entry != null)
                {
                    entries.Add(entry);
                    continue; // tag recognized
                }
                
                // Try parse exceptions

                if (exception == null) // not yet seen
                {
                    exception = TryParseAsException(key, value);
                    if (exception != null)
                        continue; // tag recognized
                }

                // Deprecated tags we recognize but consider legacy
                DeprecatedTag? deprecated = TryParseAsDeprecatedTag(key, value);
                if (deprecated != null)
                {
                    deprecatedTags.Add(deprecated);
                    continue; // tag recognized as deprecated
                }

                // Other tags that may be present, but we don't explicitly parse
                
                if (key == "note") continue;
                if (key == "fixme") continue;
                if (key == "description") continue;
                if (key == "check_date") continue;
                if (key == "source") continue;
                if (key == "implicit") continue; // todo: check value
                
                // Unknown tag
                
                unknownTags.Add(new UnknownTag(key, value));
            }
            
            // todo: parse members
            
            // todo: coord from via not average
            
            restrictions.Add(new Restriction(osmRelation, entries, unknownTags, deprecatedTags, exception));
        }
        
        // Unknown restriction value
        
        report.AddGroup(
            ReportGroup.UnknownRestrictionValues, 
            "Unknown Restriction Values",
            "These relations have `restriction` or `restriction:conditional` tags with unknown/unsupported values. " +
            "Known values are: " + string.Join(", ", _knownRestrictionValues.Select(v => "`" + v + "`")) + ". " +
            "Known conditionals are simple date/time ranges. " +
            "In general, if these are complicated conditional cases, then they are probably just not parsed correctly and need manual confirmation."
        );

        foreach (Restriction restriction in restrictions)
        {
            foreach (RestrictionEntry entry in restriction.Entries)
            {
                if (entry.Value is RestrictionUnknownValue)
                {
                    report.AddEntry(
                        ReportGroup.UnknownRestrictionValues,
                        new IssueReportEntry(
                            $"Relation has unknown restriction value `{entry.Key}={entry.Value.Value}` - " + restriction.Element.OsmViewUrl,
                            restriction.Element.AverageCoord,
                            MapPointStyle.Problem
                        )
                    );
                }
            }
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
                    string.Join(", ", restriction.UnknownTags.Select(t => $"`{t.Key}={t.Value}`")) +
                    " - " + restriction.Element.OsmViewUrl,
                    restriction.Element.AverageCoord,
                    MapPointStyle.Dubious
                )
            );
        }
        
        // Deprecated tags
        
        report.AddGroup(
            ReportGroup.DeprecatedTags, 
            "Deprecated Tags",
            "These relations have known tags, but are considered deprecated for turn restrictions. " +
            "For time window `day_on`, `day_off`, `hour_on`, `hour_off` tags, recommended use is `restriction:conditional` instead."
        );
        
        foreach (Restriction restriction in restrictions.Where(r => r.DeprecatedTags.Count > 0))
        {
            report.AddEntry(
                ReportGroup.DeprecatedTags,
                new IssueReportEntry(
                    $"Relation has {restriction.DeprecatedTags.Count} deprecated tags: " +
                    string.Join(", ", restriction.DeprecatedTags.Select(t => $"`{t.Key}={t.Value}`")) +
                    " - " + restriction.Element.OsmViewUrl,
                    restriction.Element.AverageCoord,
                    MapPointStyle.Problem
                )
            );
        }
        
        // todo: detect if redundant to :conditional ?
        
        // Unknown exception modes
        
        report.AddGroup(
            ReportGroup.UnknownExceptionModes, 
            "Unknown Exception Modes",
            "These relations have `except` tags with value(s) for unknown vehicle types / transport modes. " +
            "Known vehicle types / transport modes: " + string.Join(", ", _knownVehicleModes.Select(m => "`" + m + "`")) + "."
        );
        
        foreach (Restriction restriction in restrictions.Where(r => r.Exception != null && r.Exception.Modes.OfType<ExceptionUnknownVehicle>().Any()))
        {
            IEnumerable<ExceptionUnknownVehicle> exceptionVehicles = restriction.Exception!.Modes.OfType<ExceptionUnknownVehicle>();
            
            report.AddEntry(
                ReportGroup.UnknownExceptionModes, 
                new IssueReportEntry(
                    $"Relation has unknown exception modes: " +
                    string.Join(", ", exceptionVehicles.Select(m => "`" + m.Value + "`")) + 
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
        int hasExceptions = 0;
        int hasDeprecated = 0;
        
        foreach (Restriction restriction in restrictions)
        {
            if (restriction.Entries.Count == 0)
            {
                noTag++;
            }
            else
            {
                bool hasMainTag = restriction.Entries.Any(p => p is RestrictionPrimaryEntry);
                bool hasConditionalTag = restriction.Entries.Any(p => p is RestrictionConditionalEntry);

                if (hasMainTag && hasConditionalTag)
                    bothMainAndConditionalTag++;
                else if (hasMainTag)
                    justMainTag++;
                else if (hasConditionalTag)
                    justConditionalTag++;
                
                if (restriction.Exception != null)
                    hasExceptions++;
            }

            if (restriction.DeprecatedTags.Count > 0)
                hasDeprecated++;
        }
                
        report.AddEntry(
            ReportGroup.Stats, 
            new GenericReportEntry($"{justMainTag} are with just main `restriction` tag.")
        );
        
        report.AddEntry(
            ReportGroup.Stats, 
            new GenericReportEntry($"{justConditionalTag} are with just `restriction:conditional` tag.")
        );
        
        report.AddEntry(
            ReportGroup.Stats, 
            new GenericReportEntry($"{bothMainAndConditionalTag} are with both `restriction` and `restriction:conditional` tags.")
        );

        if (noTag > 0)
        {
            report.AddEntry(
                ReportGroup.Stats,
                new GenericReportEntry($"The remaining {noTag} have no recognized restriction tags.")
            );
        }
        
        if (hasExceptions > 0)
        {
            report.AddEntry(
                ReportGroup.Stats,
                new GenericReportEntry($"{hasExceptions} have `except` tag defining exceptions.")
            );
        }

        if (hasDeprecated > 0)
        {
            report.AddEntry(
                ReportGroup.Stats,
                new GenericReportEntry($"{hasDeprecated} have deprecated tags present.")
            );
        }
        
        // TODO
        
        // restriction:hgv, restriction:caravan, restriction:motorcar, restriction:bus, restriction:agricultural, restriction:motorcycle, restriction:bicycle, restriction:hazmat
    }

    
    [Pure]
    private static RestrictionEntry? TryParseAsEntry(string key, string value)
    {
        if (key == "restriction")
        {
            RestrictionValue restrictionValue = TryParseSimpleRestrictionValue(value);
            return new RestrictionPrimaryEntry(key, restrictionValue);
        }

        if (key == "restriction:conditional")
        {
            RestrictionValue restrictionValue = TryParseConditionalRestrictionValue(value);
            return new RestrictionConditionalEntry(key, restrictionValue);
        }

        return null;
    }

    private static readonly string[] _knownVehicleModes =
    [
        "psv",
        "bicycle",
        "hgv",
        "motorcar",
        "motorcycle",
        "bus",
        "caravan",
        "agricultural",
        "tractor",
        "emergency",
        "hazmat",
        "taxi",
        "moped"
    ];

    [Pure]
    private static RestrictionExceptions? TryParseAsException(string key, string value)
    {
        if (key != "except")
            return null;
        
        List<ExceptionVehicle> modes = [ ];

        string[] entries = value.Split(";", StringSplitOptions.TrimEntries);

        foreach (string entry in entries)
        {
            if (_knownVehicleModes.Contains(entry))
            {
                modes.Add(new ExceptionKnownVehicle(entry));
                continue;
            }
            
            modes.Add(new ExceptionUnknownVehicle(entry));
        }

        return new RestrictionExceptions(key, value, modes);
    }

    [Pure]
    private static DeprecatedTag? TryParseAsDeprecatedTag(string key, string value)
    {
        // Legacy time window scheme previously often used with restrictions
        
        if (key
            is "day_on"
            or "day_off"
            or "hour_on"
            or "hour_off")
        {
            return new DeprecatedTag(key, value);
        }

        return null;
    }

    private static readonly string[] _knownRestrictionValues =
    [
        "none", // todo: check it's not by itself
        "no_right_turn",
        "no_left_turn",
        "no_u_turn",
        "no_straight_on",
        "only_right_turn",
        "only_left_turn",
        "only_u_turn",
        "only_straight_on",
        "no_entry",
        "no_exit"
    ];
    
    private static readonly string _knownRestrictionValuesPattern = string.Join("|", _knownRestrictionValues);
    
    [Pure]
    private static RestrictionValue TryParseSimpleRestrictionValue(string value)
    {
        if (_knownRestrictionValues.Contains(value))
            return new RestrictionSimpleValue(value);
        
        return new RestrictionUnknownValue(value);
    }
    
    [Pure]
    private static RestrictionValue TryParseConditionalRestrictionValue(string value)
    {
        // Stuff like
        // "no_right_turn @ (22:00-07:00)"
        // "no_right_turn @ (Mo-Fr 07:00-09:00)"
        // "no_left_turn @ 08:00-21:00"
        // "no_left_turn @ Mo-Su 08:00-21:00"
        
        Match match = Regex.Match(value, $@"^({_knownRestrictionValuesPattern}) @ \(?(.+)\)?$");
        
        // todo: more complex, need a full-on conditional parsing then
        
        if (match.Success)
        {
            string mainValue = match.Groups[1].Value;
            string condition = match.Groups[2].Value;
            
            return new RestrictionConditionalValue(value, mainValue, condition);
            
            // todo: condition parse
        }
        
        return new RestrictionUnknownValue(value);
    }

    
    private record Restriction(
        OsmRelation Element,
        List<RestrictionEntry> Entries,
        List<UnknownTag> UnknownTags,
        List<DeprecatedTag> DeprecatedTags,
        RestrictionExceptions? Exception);
    
    
    private abstract record RestrictionEntry(string Key, RestrictionValue Value);
    
    private record RestrictionPrimaryEntry(string Key, RestrictionValue Value) : RestrictionEntry(Key, Value);
    
    private record RestrictionConditionalEntry(string Key, RestrictionValue Value) : RestrictionEntry(Key, Value);
    
    
    private abstract record RestrictionValue(string Value);
    
    private record RestrictionSimpleValue(string Value) : RestrictionValue(Value);
    
    private record RestrictionConditionalValue(string Value, string MainValue, string Condition) : RestrictionValue(Value);

    private record RestrictionUnknownValue(string Value) : RestrictionValue(Value);


    private record RestrictionExceptions(string Key, string Value, List<ExceptionVehicle> Modes);
    
    
    private abstract record ExceptionVehicle(string Value);

    private record ExceptionKnownVehicle(string Value) : ExceptionVehicle(Value);
    
    private record ExceptionUnknownVehicle(string Value) : ExceptionVehicle(Value);

    
    private record UnknownTag(string Key, string Value);
    
    // todo:
    private record DeprecatedTag(string Key, string Value);
    
    
    private enum ReportGroup
    {
        Stats,
        UnknownRestrictionValues,
        DeprecatedTags,
        UnknownTags,
        UnknownExceptionModes
    }
}
