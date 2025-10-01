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

            List<RestrictionEntry> entries = [ ];
            List<UnknownTag> unknownTags = [ ];
            List<DeprecatedTag> deprecatedTags = [ ];
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

            List<string?> modes = entries.Select(e => e.Mode).Distinct().ToList();

            List<string> baseRestrictionValues = [ ];
            foreach (RestrictionEntry entry in entries)
            {
                if (entry.Value is RestrictionSimpleValue simpleValue)
                {
                    if (!baseRestrictionValues.Contains(simpleValue.Value))
                        baseRestrictionValues.Add(simpleValue.Value);
                }
                else if (entry.Value is RestrictionConditionalValue conditionalValue)
                {
                    if (!baseRestrictionValues.Contains(conditionalValue.MainValue))
                        baseRestrictionValues.Add(conditionalValue.MainValue);
                }
            }
            
            RestrictionKind kind;
            
            // Get the single main restriction value e.g. `no_left_turn` (ignoring `none`), otherwise it's not determinable
            string? mainValue = baseRestrictionValues.SingleOrDefault(v => v != "none");
            
            if (mainValue != null)
                kind = TryParseRestrictionKind(mainValue);
            else
                kind = new MixedRestrictionKind();

            restrictions.Add(
                new Restriction(
                    osmRelation,
                    entries,
                    kind,
                    unknownTags,
                    deprecatedTags,
                    exception,
                    members,
                    fromMembers,
                    toMembers,
                    viaMembers,
                    unknownMembers,
                    modes,
                    baseRestrictionValues
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
            // Evaluate per mode (including default null mode)
            foreach (string? mode in restriction.Modes)
            {
                RestrictionPrimaryEntry? primary = restriction.Entries.OfType<RestrictionPrimaryEntry>().SingleOrDefault(e => e.Mode == mode);
                RestrictionConditionalEntry? conditional = restriction.Entries.OfType<RestrictionConditionalEntry>().SingleOrDefault(e => e.Mode == mode);

                if (primary == null || conditional == null)
                    continue;

                if (primary.Value is RestrictionSimpleValue { Value: not "none" } primaryValue &&
                    conditional.Value is RestrictionConditionalValue { MainValue: "none" } conditionalValue)
                {
                    string flipped = TryFlipConditionalValue(conditionalValue.Condition);

                    report.AddEntry(
                        ReportGroup.PossiblyFlippedConditional,
                        new IssueReportEntry(
                            $"Relation has `{primary.Key}={primaryValue.Value}` together with `{conditional.Key}=none @ {conditionalValue.Condition}`, " +
                            $"expecting simpler syntax with just `{primary.Key}:conditional={primaryValue.Value} @ {flipped}` - " +
                            restriction.Element.OsmViewUrl,
                            restriction.Element.AverageCoord,
                            MapPointStyle.Dubious
                        )
                    );
                }
            }
        }

        // Inconsistent restriction values of various kind

        report.AddGroup(
            ReportGroup.InconsistentRestrictionValues,
            "Inconsistent Restriction Values",
            "These relations have internally inconsistent values."
        );

        foreach (Restriction restriction in restrictions)
        {
            // Evaluate per mode (including default null mode)
            foreach (string? mode in restriction.Modes)
            {
                RestrictionPrimaryEntry? primary = restriction.Entries.OfType<RestrictionPrimaryEntry>().SingleOrDefault(e => e.Mode == mode);
                RestrictionConditionalEntry? conditional = restriction.Entries.OfType<RestrictionConditionalEntry>().SingleOrDefault(e => e.Mode == mode);

                // Check if both primary and conditional have the same main value
                if (primary != null &&
                    conditional != null &&
                    primary.Value is RestrictionSimpleValue primarySimpleValue &&
                    conditional.Value is RestrictionConditionalValue conditionalValue &&
                    primarySimpleValue.Value == conditionalValue.MainValue)
                {
                    report.AddEntry(
                        ReportGroup.InconsistentRestrictionValues,
                        new IssueReportEntry(
                            $"Relation has the same main value in both tags for " +
                            (mode != null ? $"mode `{mode}`" : "default mode") +
                            $": `{primary.Key}={primary.Value}` and `{conditional.Key}={conditional.Value}` - the condition is effectively redundant and unlikely intended - " +
                            restriction.Element.OsmViewUrl,
                            restriction.Element.AverageCoord,
                            MapPointStyle.Problem
                        )
                    );
                }

                // Check if primary value is `none` but there are no conditionals
                if (primary != null &&
                    conditional == null &&
                    primary.Value is RestrictionSimpleValue { Value: "none" })
                {
                    report.AddEntry(
                        ReportGroup.InconsistentRestrictionValues,
                        new IssueReportEntry(
                            $"Relation has `{primary.Key}=none` but no `{primary.Key}:conditional` entries making it pointless - " +
                            restriction.Element.OsmViewUrl,
                            restriction.Element.AverageCoord,
                            MapPointStyle.Problem
                        )
                    );
                }
            }
        }
        
        // Check that restriction doesn't define different types for different modes (e.g. `no_left_turn` for one, `no_right_turn` for another)
        foreach (Restriction restriction in restrictions)
        {
            if (restriction.BaseRestrictionValues.Count(rv => rv != "none") > 1) // ignore `none`, it's a special case of removing restriction, and it gets way too complicated to check all the possible combos
            {
                report.AddEntry(
                    ReportGroup.InconsistentRestrictionValues,
                    new IssueReportEntry(
                        $"Relation has different restriction values for different modes: " +
                        string.Join(", ", restriction.BaseRestrictionValues.Select(v => $"`{v}`")) +
                        ", expecting only one type of restriction per relation - " +
                        restriction.Element.OsmViewUrl,
                        restriction.Element.AverageCoord,
                        MapPointStyle.Problem
                    )
                );
            }
        }
        
        // Check that restriction doesn't have a default mode if it has mode-specific entries
        foreach (Restriction restriction in restrictions)
        {
            if (restriction.Modes.Count > 1 && 
                restriction.Modes.Contains(null) && 
                restriction.BaseRestrictionValues.Count == 1) // only if all modes have the same value, otherwise it's already reported above
            {
                string modes = string.Join(", ", restriction.Modes.Where(m => m != null).Select(m => $"`{m}`"));

                report.AddEntry(
                    ReportGroup.InconsistentRestrictionValues,
                    new IssueReportEntry(
                        $"Relation has both default mode and mode-specific {modes} restriction entries, which are pointless - " +
                        restriction.Element.OsmViewUrl,
                        restriction.Element.AverageCoord,
                        MapPointStyle.Problem
                    )
                );
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
            else
            {
                if (restriction.ViaMembers.Count > 1)
                {
                    // Multiple `via` is allowed, but only if all are ways (for complex junctions)
                    if (restriction.ViaMembers.OfType<RestrictionViaNodeMember>().Any())
                    {
                        roleIssues.Add("has multiple `via` members but not all are ways");
                        roleMembersFail = true; // cannot continue connectivity checks because it's fundamentally broken
                    }

                    // Repeats in `via` are not allowed
                    if (restriction.ViaMembers.Distinct().Count() != restriction.ViaMembers.Count)
                    {
                        roleIssues.Add("has repeated members in `via`");
                        roleMembersFail = true; // cannot continue connectivity checks because it's fundamentally broken
                    }
                }

                // `via` cannot repeat `from` or `to`
                if (restriction.ViaMembers.Any(v => restriction.FromMembers.Any(fm => fm.Member == v.Member)))
                {
                    roleIssues.Add("has `via` member that is the same as `from`");
                    roleMembersFail = true; // cannot continue connectivity checks because it's fundamentally broken
                }

                if (restriction.ViaMembers.Any(v => restriction.ToMembers.Any(tm => tm.Member == v.Member)))
                {
                    roleIssues.Add("has `via` member that is the same as `to`");
                    roleMembersFail = true; // cannot continue connectivity checks because it's fundamentally broken
                }

                // `from` and `to` can be the same, but this is a special case fot u-turns, not a general fail
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

            // Make sure that the elements chain to each other in order - from -> via(s) -> to

            if (!OsmAlgorithms.IsChained(
                    fromWay.Way,
                    restriction.ViaMembers.Select(vm => vm.Element).ToArray(),
                    toWay.Way
                ))
            {
                report.AddEntry(
                    ReportGroup.Connectivity,
                    new IssueReportEntry(
                        "Relation members do not connect/chain in the order `from` → `via`(s) → `to` order - " +
                        restriction.Element.OsmViewUrl,
                        restriction.Element.AverageCoord,
                        MapPointStyle.Problem
                    )
                );
            }
            else
            {
                // Properly chained, so check restriction sanity

                // todo:
            }
        }

        // Conflicting restrictions

        Dictionary<RestrictionViaNodeMember, List<Restriction>> restrictionsByViaNode = [ ];

        foreach (Restriction restriction in restrictions)
        {
            if (restriction.ViaMembers.Count == 1)
            {
                if (restriction.ViaMembers[0] is RestrictionViaNodeMember rvnm) // if it's ways, it gets way too complicated 
                {
                    if (!restrictionsByViaNode.TryGetValue(rvnm, out List<Restriction>? list))
                    {
                        list = [ ];
                        restrictionsByViaNode[rvnm] = list;
                    }

                    list.Add(restriction);
                }
            }
        }

        foreach ((RestrictionViaNodeMember via, List<Restriction> sharedRestrictions) in restrictionsByViaNode)
        {
            if (sharedRestrictions.Count > 1)
            {
                // todo:
            }
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
        int nonDefaultModes = 0;
        int mixedModes = 0;

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
            
            if (restriction.Modes.Any(m => m != null))
                nonDefaultModes++;
            
            if (restriction.Modes.Count > 1)
                mixedModes++;
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

        if (nonDefaultModes > 0)
        {
            report.AddEntry(
                ReportGroup.Stats,
                new GenericReportEntry($"{nonDefaultModes} have non-default (mode-specific) restriction tags.")
            );
        }
        
        if (mixedModes > 0)
        {
            report.AddEntry(
                ReportGroup.Stats,
                new GenericReportEntry($"{mixedModes} have mixed default and mode-specific restriction tags.")
            );
        }

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
        Dictionary<string, int> nonDefaultModeCounts = [ ];

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
                
                // Count non-default modes
                if (entry.Mode != null)
                {
                    if (!nonDefaultModeCounts.TryGetValue(entry.Mode, out int cnt))
                        nonDefaultModeCounts[entry.Mode] = 1;
                    else
                        nonDefaultModeCounts[entry.Mode] = cnt + 1;
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
        
        if (nonDefaultModeCounts.Count > 0)
        {
            List<string> parts = [ ];

            foreach (KeyValuePair<string, int> kv in nonDefaultModeCounts.OrderByDescending(kv => kv.Value))
                parts.Add($"`{kv.Key}` × {kv.Value}");

            report.AddEntry(
                ReportGroup.Stats,
                new GenericReportEntry("Non-default modes used: " + string.Join(", ", parts) + ".")
            );
        }
    }


    [Pure]
    private static RestrictionEntry? TryParseAsEntry(string key, string value)
    {
        // Parse default keys - `restriction` and `restriction:conditional`
        
        if (key == "restriction")
        {
            RestrictionValue restrictionValue = TryParseSimpleRestrictionValue(value);
            return new RestrictionPrimaryEntry(key, null, restrictionValue);
        }

        if (key == "restriction:conditional")
        {
            RestrictionValue restrictionValue = TryParseConditionalRestrictionValue(value);
            return new RestrictionConditionalEntry(key, null, restrictionValue);
        }

        // Parse mode-specific keys like `restriction:hgv` and `restriction:hgv:conditional`
        
        if (key.StartsWith("restriction:"))
        {
            string[] parts = key.Split(':');

            if (parts.Length == 2)
            {
                string mode = parts[1];
                if (!_knownVehicleModes.Contains(mode))
                    return null; // not a transport mode

                RestrictionValue restrictionValue = TryParseSimpleRestrictionValue(value);
                return new RestrictionPrimaryEntry(key, mode, restrictionValue);
            }

            if (parts.Length == 3 && parts[2] == "conditional")
            {
                string mode = parts[1];
                if (!_knownVehicleModes.Contains(mode))
                    return null; // not a transport mode

                RestrictionValue restrictionValue = TryParseConditionalRestrictionValue(value);
                return new RestrictionConditionalEntry(key, mode, restrictionValue);
            }
        }
        
        // todo: if ever needed, could parse out stuff like `restriction:source` or `restriction:note` or whatever

        // Not a restriction entry we understand
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
    
    [Pure]
    private static RestrictionKind TryParseRestrictionKind(string value)
    {
        switch (value)
        {
            case "no_right_turn":
            case "no_left_turn":
            case "no_straight_on":
                return new NoDirectionRestriction(value);
            
            case "only_right_turn":
            case "only_left_turn":
            case "only_straight_on":
                return new OnlyDirectionRestriction(value);
            
            case "no_u_turn":
                return new NoUTurnRestriction(value);
            
            case "only_u_turn":
                return new OnlyUTurnRestriction(value);
            
            case "no_entry":
            case "no_exit":
                return new NoPassRestriction(value);
            
            case "none":
                throw new Exception("Restriction kind cannot be 'none'");
            
            default:
                return new UnknownRestrictionKind(value);
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="Element"></param>
    /// <param name="Entries"></param>
    /// <param name="UnknownTags"></param>
    /// <param name="DeprecatedTags"></param>
    /// <param name="Exception"></param>
    /// <param name="Members"></param>
    /// <param name="FromMembers"></param>
    /// <param name="ToMembers"></param>
    /// <param name="ViaMembers"></param>
    /// <param name="UnknownMembers"></param>
    /// <param name="Modes"></param>
    /// <param name="BaseRestrictionValues">Different values used like `no_left_turn`, `no_right_turn` etc. Valid (parsable) restriction should have only 1.</param>
    private record Restriction(
        OsmRelation Element,
        List<RestrictionEntry> Entries,
        RestrictionKind Kind,
        List<UnknownTag> UnknownTags,
        List<DeprecatedTag> DeprecatedTags,
        RestrictionExceptions? Exception,
        List<RestrictionMember> Members,
        List<RestrictionFromMember> FromMembers,
        List<RestrictionToMember> ToMembers,
        List<RestrictionViaMember> ViaMembers,
        List<RestrictionUnknownMember> UnknownMembers,
        List<string?> Modes,
        List<string> BaseRestrictionValues);


    private abstract record RestrictionMember(OsmRelationMember Member);

    private record RestrictionFromMember(OsmRelationMember Member, OsmWay Way) : RestrictionMember(Member);

    private record RestrictionToMember(OsmRelationMember Member, OsmWay Way) : RestrictionMember(Member);

    private abstract record RestrictionViaMember(OsmRelationMember Member, OsmElement Element) : RestrictionMember(Member);

    private record RestrictionViaNodeMember(OsmRelationMember Member, OsmNode Node) : RestrictionViaMember(Member, Node);

    private record RestrictionViaWayMember(OsmRelationMember Member, OsmWay Way) : RestrictionViaMember(Member, Way);

    private record RestrictionUnknownMember(OsmRelationMember Member, string Role) : RestrictionMember(Member);


    /// <summary>
    /// A single restriction entry - OSM tag (key + value), either primary (i.e. `restriction`) or conditional (i.e. `restriction:conditional`).
    /// </summary>
    /// <param name="Key">The OSM full key</param>
    /// <param name="Mode">The mode of transport as a subkey of the Key; null if default/all</param>
    /// <param name="Value">The OSM full value of the tag</param>
    private abstract record RestrictionEntry(string Key, string? Mode, RestrictionValue Value);

    private record RestrictionPrimaryEntry(string Key, string? Mode, RestrictionValue Value) : RestrictionEntry(Key, Mode, Value);

    private record RestrictionConditionalEntry(string Key, string? Mode, RestrictionValue Value) : RestrictionEntry(Key, Mode, Value);


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
    
    
    private abstract record RestrictionKind;
    
    private abstract record KnownRestrictionKind(string Value) : RestrictionKind;

    private record UnknownRestrictionKind(string Value) : RestrictionKind;
    
    private record MixedRestrictionKind : RestrictionKind;

    /// <summary>
    /// Restriction that only allows something (like `only_right_turn` or `only_u_turn`).
    /// </summary>
    private abstract record OnlyRestriction(string Value) : KnownRestrictionKind(Value);

    /// <summary>
    /// Restriction that restricts a specific something (like `no_left_turn` or `no_u_turn`).
    /// </summary>
    private abstract record NoRestriction(string Value) : KnownRestrictionKind(Value);
    
    /// <summary>
    /// Restriction that only allows a specific direction (like `only_right_turn`).
    /// </summary>
    private record OnlyDirectionRestriction(string Value) : OnlyRestriction(Value);
    
    /// <summary>
    /// Restriction that restricts a specific direction (like `no_left_turn`).
    /// </summary>
    private record NoDirectionRestriction(string Value) : NoRestriction(Value);
    
    /// <summary>
    /// Restriction that only allows u-turns (i.e. `only_u_turn`).
    /// </summary>
    private record OnlyUTurnRestriction(string Value) : OnlyRestriction(Value);

    /// <summary>
    /// Restriction that restricts u-turns (i.e. `no_u_turn`).
    /// </summary>
    private record NoUTurnRestriction(string Value) : NoRestriction(Value);

    /// <summary>
    /// Restriction that restricts entry or exit (i.e. `no_entry` or `no_exit`).
    /// </summary>
    private record NoPassRestriction(string Value) : KnownRestrictionKind(Value);
    


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
