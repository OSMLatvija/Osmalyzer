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
            new RelationMustHaveAllMembersDownloaded(),
            new HasAnyValue("type", "restriction"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.Fuzzy)
        );
        
        // Parse

        List<Restriction> restrictions = [ ];

        foreach (OsmRelation osmRelation in restrictionRelations.Relations)
        {
            // Tags
            
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
            
            // Member elements
            
            List<RestrictionMember> members = [ ];

            foreach (OsmRelationMember relationMember in osmRelation.Members)
            {
                switch (relationMember.Role)
                {
                    case "from":
                        if (relationMember.Element is OsmWay fw)
                            members.Add(new RestrictionFromMember(relationMember, fw));
                        else
                            members.Add(new RestrictionUnknownMember(relationMember, relationMember.Role)); // `from` must be way
                        break;
                    
                    case "to":
                        if (relationMember.Element is OsmWay tw)
                            members.Add(new RestrictionToMember(relationMember, tw));
                        else
                            members.Add(new RestrictionUnknownMember(relationMember, relationMember.Role)); // `to` must be way
                        break;
                    
                    case "via":
                        if (relationMember.Element is OsmNode vn)
                            members.Add(new RestrictionViaNodeMember(relationMember, vn));
                        else if (relationMember.Element is OsmWay vw)
                            members.Add(new RestrictionViaWayMember(relationMember, vw));
                        else
                            members.Add(new RestrictionUnknownMember(relationMember, relationMember.Role)); // `via` must be node or way
                        break;
                    
                    default:
                        members.Add(new RestrictionUnknownMember(relationMember, relationMember.Role));
                        break;
                }
            }
            
            List<RestrictionFromMember> fromMembers = members.OfType<RestrictionFromMember>().ToList();
            List<RestrictionToMember> toMembers = members.OfType<RestrictionToMember>().ToList();
            List<RestrictionViaMember> viaMembers = members.OfType<RestrictionViaMember>().ToList();
            List<RestrictionUnknownMember> unknownMembers = members.OfType<RestrictionUnknownMember>().ToList();

            // todo: coord from via if possible not average
            
            restrictions.Add(
                new Restriction(
                    osmRelation,
                    entries,
                    unknownTags,
                    deprecatedTags,
                    exception,
                    members,
                    fromMembers,
                    toMembers,
                    viaMembers,
                    unknownMembers
                )
            );
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
                    "Relation has unknown exception modes: " +
                    string.Join(", ", exceptionVehicles.Select(m => "`" + m.Value + "`")) + 
                    " - " + restriction.Element.OsmViewUrl,
                    restriction.Element.AverageCoord,
                    MapPointStyle.Dubious
                )
            );
        }
        
        // "Flipped" restriction logic

        report.AddGroup(
            ReportGroup.PossiblyFlippedConditional,
            @"""Flipped"" Conditionals",
            @"These relations have a main `restriction=*` together with uncommon `restriction:conditional=none @ …`. " +
            @"Usually it's expected these to be in reverse to match the traffic signage usage (e.g. ""no left turns during these hours""). " +
            "These are however not logically incorrect, just more convoluted as tagging for the renderer, " +
            "prioritizing the common hour restriction over the off-hour allowance to aid routers that only read the main `restriction`."
        );

        foreach (Restriction restriction in restrictions)
        {
            // Exactly 1 of each
            RestrictionPrimaryEntry? primary = restriction.Entries.OfType<RestrictionPrimaryEntry>().SingleOrDefault();
            RestrictionConditionalEntry? conditional = restriction.Entries.OfType<RestrictionConditionalEntry>().SingleOrDefault();

            if (primary == null || conditional == null)
                continue;
            
            if (primary.Value is RestrictionSimpleValue { Value: not "none" } primaryValue &&
                conditional.Value is RestrictionConditionalValue { MainValue: "none" } conditionalValue)
            {
                string flipped = TryFlipConditionalValue(conditionalValue.Condition);
                
                report.AddEntry(
                    ReportGroup.PossiblyFlippedConditional,
                    new IssueReportEntry(
                        $"Relation has `restriction={primaryValue.Value}` together with `restriction:conditional=none @ {conditionalValue.Condition}`, " +
                        $"expecting simpler syntax with just `restriction:conditional={primaryValue.Value} @ {flipped}` - " +
                        restriction.Element.OsmViewUrl,
                        restriction.Element.AverageCoord,
                        MapPointStyle.Dubious
                    )
                );
            }
        }
        
        // Inconsistent values of various kind
        
        report.AddGroup(
            ReportGroup.InconsistentRestrictionValues,
            "Inconsistent Restriction Values",
            "These relations have internally inconsistent values."
        );

        foreach (Restriction restriction in restrictions)
        {
            RestrictionPrimaryEntry? primary = restriction.Entries.OfType<RestrictionPrimaryEntry>().SingleOrDefault();
            List<RestrictionConditionalEntry> conditionals = restriction.Entries.OfType<RestrictionConditionalEntry>().ToList();

            if (primary == null || conditionals.Count == 0)
                continue;

            if (primary.Value is RestrictionSimpleValue pv)
            {
                // Find all conditional entries that conflict with the primary (same main value)
                List<RestrictionConditionalValue> conflicting = conditionals
                    .Select(c => c.Value)
                    .OfType<RestrictionConditionalValue>()
                    .Where(cv => cv.MainValue == pv.Value)
                    .ToList();

                if (conflicting.Count > 0)
                {
                    string parts = string.Join(
                        ", ",
                        conflicting.Select(cv => $"`restriction:conditional={cv.MainValue} @ {cv.Condition}`")
                    );

                    report.AddEntry(
                        ReportGroup.InconsistentRestrictionValues,
                        new IssueReportEntry(
                            $"Relation has the same main value in both tags: `restriction={pv.Value}` and {parts} - the condition is effectively redundant and unlikely intended - " +
                            restriction.Element.OsmViewUrl,
                            restriction.Element.AverageCoord,
                            MapPointStyle.Problem
                        )
                    );
                }
            }
        }
        
        // Connectivity and member checks
        
        report.AddGroup(
            ReportGroup.Connectivity,
            "Member Connectivity",
            "These relations have problems with their members and connectivity."
        );

        foreach (Restriction restriction in restrictions)
        {
            // Invalid roles/members (report regardless, although connectivity might still exist fine ignoring these) 
            
            foreach (RestrictionUnknownMember unknownMember in restriction.UnknownMembers)
            {
                report.AddEntry(
                    ReportGroup.Connectivity,
                    new IssueReportEntry(
                        $"Member with invalid or unexpected combo of role `{unknownMember.Role}` and type `{unknownMember.Member.Element!.ElementType.ToString().ToLower()}` - " + restriction.Element.OsmViewUrl,
                        restriction.Element.AverageCoord,
                        MapPointStyle.Problem
                    )
                );
            }
            
            // Determine main restriction kind (turn vs uturn), if known
            // todo: more generic during parsing
            
            string? mainValue = null;
            {
                RestrictionEntry? primary = restriction.Entries.OfType<RestrictionPrimaryEntry>().FirstOrDefault();
                if (primary?.Value is RestrictionSimpleValue psv) mainValue = psv.Value;
                else
                {
                    RestrictionConditionalEntry? cond = restriction.Entries.OfType<RestrictionConditionalEntry>().FirstOrDefault();
                    if (cond?.Value is RestrictionConditionalValue cv) mainValue = cv.MainValue;
                    else if (cond?.Value is RestrictionSimpleValue csv) mainValue = csv.Value; // unlikely
                }
            }
            bool isUTurn = mainValue is "no_u_turn" or "only_u_turn";
            bool isTurn = mainValue is "no_left_turn" or "no_right_turn" or "no_straight_on" or "only_left_turn" or "only_right_turn" or "only_straight_on";
            bool valueKnown = mainValue != null && _knownRestrictionValues.Contains(mainValue);

            // Missing or multiple critical members

            bool roleMembersFail = false;
            List<string> roleIssues = [ ];
            
            if (restriction.FromMembers.Count == 0)
            {
                roleIssues.Add("is missing `from` member (way)");
                roleMembersFail = true; // cannot continue connectivity checks because it's fundamentally broken
            }
            else if (restriction.FromMembers.Count > 1)
            {
                roleIssues.Add($"has multiple `from` members ({restriction.FromMembers.Count})");
                roleMembersFail = true; // cannot continue connectivity checks because it's fundamentally broken
            }

            if (restriction.ToMembers.Count == 0)
            {
                roleIssues.Add("is missing `to` member (way)");
                roleMembersFail = true; // cannot continue connectivity checks because it's fundamentally broken
            }
            else if (restriction.ToMembers.Count > 1)
            {
                roleIssues.Add($"has multiple `to` members ({restriction.ToMembers.Count})");
                roleMembersFail = true; // cannot continue connectivity checks because it's fundamentally broken
            }

            if (restriction.ViaMembers.Count == 0)
            {
                roleIssues.Add("is missing `via` member (node or way)");
                roleMembersFail = true; // cannot continue connectivity checks because it's fundamentally broken
            }
            else if (restriction.ViaMembers.Count > 1 && restriction.ViaMembers.OfType<RestrictionViaNodeMember>().Any())
            {
                roleIssues.Add("has multiple `via` members but not all are ways");
                roleMembersFail = true; // cannot continue connectivity checks because it's fundamentally broken
            }

            // If basic membership is broken, don't continue with connectivity checks, everything below relies on valid members
            if (roleMembersFail)
            {
                report.AddEntry(
                    ReportGroup.Connectivity,
                    new IssueReportEntry(
                        "Relation " +
                        string.Join(", ", roleIssues) +
                        " - " + restriction.Element.OsmViewUrl,
                        restriction.Element.AverageCoord,
                        MapPointStyle.Problem
                    )
                );
                
                continue;
            }

            // At this point we definitely have exactly one `from` and one `to`, and at least one `via`
            RestrictionFromMember fromWay = restriction.FromMembers[0];
            RestrictionToMember toWay = restriction.ToMembers[0];
            
            // If it's a turn (not u-turn), require from != to
            
            if (valueKnown && isTurn)
            {
                if (fromWay == toWay)
                {
                    report.AddEntry(
                        ReportGroup.Connectivity,
                        new IssueReportEntry(
                            "Relation has `from` and `to` that are the same way - " + restriction.Element.OsmViewUrl,
                            restriction.Element.AverageCoord,
                            MapPointStyle.Problem
                        )
                    );
                    // Still continue to connectivity checks; this is orthogonal
                }
            }
            
            // Make sure that the elements chain to each other in order - from -> via(s) -> to
            
            // todo:
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
        
        // Unique restriction and condition values
        Dictionary<string, int> restrictionValueCounts = [ ];
        Dictionary<string, int> conditionValueCounts = [ ];

        foreach (Restriction restriction in restrictions)
        {
            foreach (RestrictionEntry entry in restriction.Entries)
            {
                switch (entry.Value)
                {
                    case RestrictionConditionalValue cval:
                    {
                        // Count main value from conditional
                        if (!restrictionValueCounts.TryGetValue(cval.MainValue, out int cnt1))
                            restrictionValueCounts[cval.MainValue] = 1;
                        else
                            restrictionValueCounts[cval.MainValue] = cnt1 + 1;

                        // Count condition string
                        if (!conditionValueCounts.TryGetValue(cval.Condition, out int cnt2))
                            conditionValueCounts[cval.Condition] = 1;
                        else
                            conditionValueCounts[cval.Condition] = cnt2 + 1;
                        break;
                    }
                    
                    case RestrictionSimpleValue:
                    {
                        // Primary or unknown values
                        string v = entry.Value.Value;
                        if (!restrictionValueCounts.TryGetValue(v, out int cnt))
                            restrictionValueCounts[v] = 1;
                        else
                            restrictionValueCounts[v] = cnt + 1;
                        break;
                    }
                    
                    case RestrictionUnknownValue:
                        // Ignore unknown values here, they get fully reported anyway
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        if (restrictionValueCounts.Count > 0)
        {
            List<string> parts = [ ];
            
            foreach (KeyValuePair<string, int> kv in restrictionValueCounts.OrderByDescending(kv => kv.Value))
                parts.Add($"`{kv.Key}` × {kv.Value}");
            
            report.AddEntry(
                ReportGroup.Stats,
                new GenericReportEntry("Restriction values used: " + string.Join(", ", parts) + ".")
            );
        }

        if (conditionValueCounts.Count > 0)
        {
            List<string> parts = [ ];
            
            foreach (KeyValuePair<string, int> kv in conditionValueCounts.OrderByDescending(kv => kv.Value))
                parts.Add($"`{kv.Key}` × {kv.Value}");
            
            report.AddEntry(
                ReportGroup.Stats,
                new GenericReportEntry("Conditional conditions used: " + string.Join(", ", parts) + ".")
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
        
        Match match = Regex.Match(value, $@"^({_knownRestrictionValuesPattern}) @ \((.+)\)$");
        
        if (!match.Success) // try without brackets (can't do in 1 go) 
            match = Regex.Match(value, $@"^({_knownRestrictionValuesPattern}) @ (.+)$");
        
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

    [Pure]
    private static string TryFlipConditionalValue(string value)
    {
        // Make "22:00-07:00" into "07:00-22:00"

        Match match = Regex.Match(value, @"^(\d{1,2}:\d{2})-(\d{1,2}:\d{2})$");
        
        if (match.Success)
        {
            string from = match.Groups[1].Value;
            string to = match.Groups[2].Value;
            
            return $"{to}-{from}";
        }
        
        // Don't know how to do anything else, but no other live example as of making this
        return "…";
    }

    
    private record Restriction(
        OsmRelation Element,
        List<RestrictionEntry> Entries,
        List<UnknownTag> UnknownTags,
        List<DeprecatedTag> DeprecatedTags,
        RestrictionExceptions? Exception,
        List<RestrictionMember> Members,
        List<RestrictionFromMember> FromMembers,
        List<RestrictionToMember> ToMembers,
        List<RestrictionViaMember> ViaMembers,
        List<RestrictionUnknownMember> UnknownMembers);
    
    
    private abstract record RestrictionMember(OsmRelationMember Member);

    private record RestrictionFromMember(OsmRelationMember Member, OsmWay Way) : RestrictionMember(Member);
    
    private record RestrictionToMember(OsmRelationMember Member, OsmWay Way) : RestrictionMember(Member);
    
    private abstract record RestrictionViaMember(OsmRelationMember Member, OsmElement Element) : RestrictionMember(Member);
    private record RestrictionViaNodeMember(OsmRelationMember Member, OsmNode Node) : RestrictionViaMember(Member, Node);
    private record RestrictionViaWayMember(OsmRelationMember Member, OsmWay Way) : RestrictionViaMember(Member, Way);
    
    private record RestrictionUnknownMember(OsmRelationMember Member, string Role) : RestrictionMember(Member);


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
        InconsistentRestrictionValues,
        UnknownExceptionModes,
        PossiblyFlippedConditional,
        Connectivity
    }
}
