namespace Osmalyzer;

[UsedImplicitly]
public class MaxspeedTypeAnalyzer : Analyzer
{
    public override string Name => "Maxspeed Types";

    public override string Description => "The report checks validity of `maxspeed:type` (and its variants) values.";

    public override AnalyzerGroup Group => AnalyzerGroup.Roads;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData) ];

        
    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;
        
        OsmDataExtract elementsWithMaxspeedType = osmMasterData.Filter(
            new HasKeyPrefixed("maxspeed:"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.Fuzzy)
        );
        
        // Gather tags
        
        const string modes = "hgv|motorcar|motorcycle|bus|bicycle"; // what is possible with standard traffic signs for Latvia (at least not seen exceptions (yet))
        
        MaxspeedTypeLayout[] recognizedLayouts = [
            new MaxspeedTypeLayout("maxspeed:type", "^maxspeed:type$"),
            new MaxspeedTypeLayout("maxspeed:type:conditional", "^maxspeed:type:conditional$"),
            new MaxspeedTypeLayout("maxspeed:type:_direction_", "^maxspeed:type:(forward|backward)$"),
            new MaxspeedTypeLayout("maxspeed:type:_direction_:conditional", "^maxspeed:type:(forward|backward):conditional$"),
            new MaxspeedTypeLayout("maxspeed:_mode_:type", $"^maxspeed:({modes}):type$"),
            new MaxspeedTypeLayout("maxspeed:_mode_:type:conditional", $"^maxspeed:({modes}):type:conditional$"),
            new MaxspeedTypeLayout("maxspeed:_mode_:type:_direction_", $"^maxspeed:({modes}):type:(forward|backward)$"),
            new MaxspeedTypeLayout("maxspeed:_mode_:type:_direction_:conditional", $"^maxspeed:({modes}):type:(forward|backward):conditional$"),
            new MaxspeedTypeLayout("maxspeed:type:advisory", $"^maxspeed:type:advisory$")
        ];
        
        List<TaggedElement> taggedElements = GatherTaggedElements(elementsWithMaxspeedType, recognizedLayouts);
        
        List<RecognizedTaggedElement> recognizedElements = taggedElements.OfType<RecognizedTaggedElement>().ToList();
        List<UnrecognizedTaggedElement> unrecognizedElements = taggedElements.OfType<UnrecognizedTaggedElement>().ToList();

        // Parse
        
        // Check that maxspeed:type is only on ways and not on nodes or relations
        
        List<RecognizedTaggedElement> nonWaysWithMaxspeedType = recognizedElements.Where(te => te.Element.ElementType != OsmElement.OsmElementType.Way).ToList();

        report.AddGroup(
            ReportGroup.UnexpectedElements, 
            "Elements where `maxspeed:type` is not expected"
        );

        foreach (RecognizedTaggedElement taggedElement in nonWaysWithMaxspeedType)
        {
            report.AddEntry(
                ReportGroup.UnexpectedElements,
                new IssueReportEntry(
                    "This " + TypeToLabel(taggedElement.Element.ElementType) + " has a `" + taggedElement.Key + "=" + taggedElement.Value + "`, but this tag is only expected on ways (roads). " + taggedElement.Element.OsmViewUrl,
                    taggedElement.Element.AverageCoord,
                    MapPointStyle.Problem
                )
            );
        }
        
        // Check that maxspeed:type is only on highways
        
        List<RecognizedTaggedElement> waysWithMaxspeedType = recognizedElements.Where(te => te.Element.ElementType == OsmElement.OsmElementType.Way).ToList();
        
        List<RecognizedTaggedElement> nonHighwaysWithMaxspeedType = waysWithMaxspeedType.Where(te => !te.Element.HasKey("highway")).ToList();
        
        foreach (RecognizedTaggedElement taggedElement in nonHighwaysWithMaxspeedType)
        {
            report.AddEntry(
                ReportGroup.UnexpectedElements,
                new IssueReportEntry(
                    "This way has a `\" + taggedElement.Key + \"=" + taggedElement.Value + "`, but this tag is only expected on `highway`s (roads). " + taggedElement.Element.OsmViewUrl,
                    taggedElement.Element.AverageCoord,
                    MapPointStyle.Problem
                )
            );
        }
        
        // Now check actual maxspeed:type values
        
        List<RecognizedTaggedElement> highwaysWithMaxspeedType = waysWithMaxspeedType.Where(te => te.Element.HasKey("highway")).ToList();
        
        MaxspeedTypeVariantDefinition[] validMaxspeedTypes = [ 
            new MaxspeedTypeVariantDefinition(MaxspeedTypeVariant.Sign, "sign", "sign"),
            new MaxspeedTypeVariantDefinition(MaxspeedTypeVariant.Urban, "LV:urban", "LV:urban"),
            new MaxspeedTypeVariantDefinition(MaxspeedTypeVariant.Rural, "LV:rural", "LV:rural"),
            new MaxspeedTypeVariantDefinition(MaxspeedTypeVariant.LivingStreet, "LV:living_street", "LV:living_street"),
            new MaxspeedTypeVariantDefinition(MaxspeedTypeVariant.Zone, "LV:zone([0-9]{1,3})", "LV:zone##"),
            new MaxspeedTypeVariantDefinition(MaxspeedTypeVariant.Construction, "construction", "construction"),
            new MaxspeedTypeVariantDefinition(MaxspeedTypeVariant.ParkingLot, "LV:parking", "LV:parking"),
            new MaxspeedTypeVariantDefinition(MaxspeedTypeVariant.FuelStation, "LV:fuel_station", "LV:fuel_station")
        ];
        
        report.AddGroup(
            ReportGroup.InvalidValues, 
            "Highways with invalid (unrecognized) maxspeed:type or its variant values.",
            "Valid/known values are: " + string.Join(", ", validMaxspeedTypes.Select(v => "`" + v.DisplayLabel + "`")) + ". Note that it hasn't been decided what value to use for apartment courtyards."
        );
        
        report.AddGroup(
            ReportGroup.MismatchedValues, 
            "Elements where maxspeed:type or its variants does not match corresponding maxspeed value"
        );
        
        report.AddGroup(
            ReportGroup.MissingMaxspeed, 
            "Elements where maxspeed:type or its variants is set but corresponding maxspeed is missing"
        );
        
        report.AddGroup(
            ReportGroup.InvalidMaxspeed, 
            "Elements where maxspeed:type or its variants is set but corresponding maxspeed is invalid/unrecognized"
        );
        
        foreach (RecognizedTaggedElement taggedElement in highwaysWithMaxspeedType)
        {
            MaxspeedTypeVariantMatch? match = MatchMaxspeedTypeValue(taggedElement.Key, taggedElement.Value, validMaxspeedTypes);
            
            if (match == null)
            {
                // Not valid/recognized, report
                
                report.AddEntry(
                    ReportGroup.InvalidValues,
                    new IssueReportEntry(
                        "This highway has an unrecognized `" + taggedElement.Key + "=" + taggedElement.Value + "`. " + taggedElement.Element.OsmViewUrl,
                        taggedElement.Element.AverageCoord,
                        MapPointStyle.Problem
                    )
                );
            }
            else
            {
                // Valid; now check against maxspeed value

                string maxspeedKey = GetMaxspeedKeyForTypedKey(taggedElement);
                string? maxspeedValue = taggedElement.Element.GetValue(maxspeedKey);

                if (maxspeedValue != null)
                {
                    if (ExtractMaxspeedValue(maxspeedValue, out int? maxspeed))
                    {
                        switch (match.Variant)
                        {
                            case MaxspeedTypeVariant.Sign:
                                break;

                            case MaxspeedTypeVariant.Urban:
                                if (maxspeed != 50)
                                {
                                    // Mismatched, report
                                    
                                    report.AddEntry(
                                        ReportGroup.MismatchedValues,
                                        new IssueReportEntry(
                                            "This highway has a `" + taggedElement.Key + "=" + taggedElement.Value + "`, but the corresponding `" + maxspeedKey + "=" + maxspeedValue + "` value does not match. Expected `" + maxspeedKey + "=50`. " + taggedElement.Element.OsmViewUrl,
                                            taggedElement.Element.AverageCoord,
                                            MapPointStyle.Problem
                                        )
                                    );
                                }
                                break;

                            case MaxspeedTypeVariant.Rural:
                                if (maxspeed != 90 && maxspeed != 80)
                                {
                                    // Mismatched, report
                                    
                                    report.AddEntry(
                                        ReportGroup.MismatchedValues,
                                        new IssueReportEntry(
                                            "This highway has a `" + taggedElement.Key + "=" + taggedElement.Value + "`, but the corresponding `" + maxspeedKey + "=" + maxspeedValue + "` value does not match. Expected `" + maxspeedKey + "=90` or `80`. " + taggedElement.Element.OsmViewUrl,
                                            taggedElement.Element.AverageCoord,
                                            MapPointStyle.Problem
                                        )
                                    );
                                }
                                break;

                            case MaxspeedTypeVariant.LivingStreet:
                                if (maxspeed != 20)
                                {
                                    // Mismatched, report
                                    
                                    report.AddEntry(
                                        ReportGroup.MismatchedValues,
                                        new IssueReportEntry(
                                            "This highway has a `" + taggedElement.Key + "=" + taggedElement.Value + "`, but the corresponding `" + maxspeedKey + "=" + maxspeedValue + "` value does not match. Expected `" + maxspeedKey + "=20`. " + taggedElement.Element.OsmViewUrl,
                                            taggedElement.Element.AverageCoord,
                                            MapPointStyle.Problem
                                        )
                                    );
                                }
                                break;

                            case MaxspeedTypeVariant.Zone:
                                if (maxspeed != match.Value!.Value)
                                {
                                    // Mismatched, report
                                    
                                    report.AddEntry(
                                        ReportGroup.MismatchedValues,
                                        new IssueReportEntry(
                                            "This highway has a `" + taggedElement.Key + "=" + taggedElement.Value + "`, but the corresponding `" + maxspeedKey + "=" + maxspeedValue + "` value does not match. Expected `" + maxspeedKey + "=" + match.Value.Value + "`. " + taggedElement.Element.OsmViewUrl,
                                            taggedElement.Element.AverageCoord,
                                            MapPointStyle.Problem
                                        )
                                    );
                                }
                                break;
                            
                            case MaxspeedTypeVariant.Construction:
                                break;

                            case MaxspeedTypeVariant.FuelStation:
                                if (maxspeed != 20)
                                {
                                    // Mismatched, report
                                    
                                    report.AddEntry(
                                        ReportGroup.MismatchedValues,
                                        new IssueReportEntry(
                                            "This highway has a `" + taggedElement.Key + "=" + taggedElement.Value + "`, but the corresponding `" + maxspeedKey + "=" + maxspeedValue + "` value does not match. Expected `" + maxspeedKey + "=20`. " + taggedElement.Element.OsmViewUrl,
                                            taggedElement.Element.AverageCoord,
                                            MapPointStyle.Problem
                                        )
                                    );
                                }
                                break;

                            case MaxspeedTypeVariant.ParkingLot:
                                if (maxspeed != 20)
                                {
                                    // Mismatched, report
                                    
                                    report.AddEntry(
                                        ReportGroup.MismatchedValues,
                                        new IssueReportEntry(
                                            "This highway has a `" + taggedElement.Key + "=" + taggedElement.Value + "`, but the corresponding `" + maxspeedKey + "=" + maxspeedValue + "` value does not match. Expected `" + maxspeedKey + "=20`. " + taggedElement.Element.OsmViewUrl,
                                            taggedElement.Element.AverageCoord,
                                            MapPointStyle.Problem
                                        )
                                    );
                                }
                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    else
                    {
                        // Invalid maxspeed value, report
                        
                        report.AddEntry(
                            ReportGroup.InvalidMaxspeed,
                            new IssueReportEntry(
                                "This highway has a `" + taggedElement.Key + "=" + taggedElement.Value + "`, but the corresponding `" + maxspeedKey + "=" + maxspeedValue + "` value is invalid/unrecognized. " + taggedElement.Element.OsmViewUrl,
                                taggedElement.Element.AverageCoord,
                                MapPointStyle.Problem
                            )
                        );
                    }
                }
                else
                {
                    // Missing maxspeed, report
                    
                    report.AddEntry(
                        ReportGroup.MissingMaxspeed,
                        new IssueReportEntry(
                            "This highway has a `" + taggedElement.Key + "=" + taggedElement.Value + "`, but is missing the corresponding `" + maxspeedKey + "` value. " + taggedElement.Element.OsmViewUrl,
                            taggedElement.Element.AverageCoord,
                            MapPointStyle.Problem
                        )
                    );
                }
            }
        }
        
        // Report unrecognized layouts
        
        report.AddGroup(
            ReportGroup.UnrecognizedLayouts, 
            "Elements where `maxspeed:type` or its variants is set with unrecognized layout (key format)",
            "Recognized layouts are: " + string.Join(", ", recognizedLayouts.Select(rl => "`" + rl.TagLabel + "`")) + ", " +
            "where direction is either `forward` or `backward` and mode is one of " + string.Join(", ", modes.Split('|').Select(m => "`" + m + "`")) + ". " +
            "There are other rare tag combinations that are valid on OSM, so these are not necessarily wrong."
        );
        
        foreach (UnrecognizedTaggedElement taggedElement in unrecognizedElements)
        {
            report.AddEntry(
                ReportGroup.UnrecognizedLayouts,
                new IssueReportEntry(
                    "This highway has an unrecognized layout/format of the maxspeed type tag: `" + taggedElement.Key + "=" + taggedElement.Value + "`. " + taggedElement.Element.OsmViewUrl,
                    taggedElement.Element.AverageCoord,
                    MapPointStyle.Problem
                )
            );
        }
        
        // Show some stats
        
        report.AddGroup(
            ReportGroup.Stats, 
            "Statistics"
        );
        
        report.AddEntry(
            ReportGroup.Stats,
            new GenericReportEntry("Total elements with recognized `maxspeed:type` or its variants: " + recognizedElements.Count)
        );

        List<KeyValuePair<string, int>> countedKeys = recognizedElements.CountBy(re => re.Key).ToList();
        
        report.AddEntry(
            ReportGroup.Stats,
            new GenericReportEntry("These keys were found: " + string.Join("; ", countedKeys.OrderByDescending(kv => kv.Value).Select(kv => " `" + kv.Key + "` × " + kv.Value)))
        );
    }

    private static bool ExtractMaxspeedValue(string maxspeedValue, out int? maxspeed)
    {
        // Direct value like `50` or something, kmh assumed 
        if (int.TryParse(maxspeedValue, out int maxspeedParsed))
        {
            maxspeed = maxspeedParsed;
            return true;
        }

        // Conditional like `30 @ (Mo-Fr 07:00-19:00)` or something
        Match match = Regex.Match(maxspeedValue, @"^([0-9]{1,3})\s*@");
        if (match.Success)
        {
            maxspeed = int.Parse(match.Groups[1].Value);
            return true;
        }
        
        maxspeed = null;
        return false;
    }

    private static string GetMaxspeedKeyForTypedKey(RecognizedTaggedElement taggedElement)
    {
        return taggedElement.Key.Replace(":type", ""); // hacky, but technically this works for all layouts
    }

    [Pure]
    private static List<TaggedElement> GatherTaggedElements(OsmDataExtract data, MaxspeedTypeLayout[] recognizedLayouts)
    {
        List<TaggedElement> taggedElements = [ ];
        
        foreach (OsmElement element in data.Elements)
            foreach ((string key, string value) in element.AllTags!)
                if (key.StartsWith("maxspeed:") && key.Contains(":type"))
                    if (DetermineTypeLayout(key, recognizedLayouts, out MaxspeedTypeLayout? layout))
                        taggedElements.Add(new RecognizedTaggedElement(element, key, value, layout!));
                    else
                        taggedElements.Add(new UnrecognizedTaggedElement(element, key, value));
        
        return taggedElements;
    }
    
    [Pure]
    private static bool DetermineTypeLayout(string key, MaxspeedTypeLayout[]? recognizedLayouts, out MaxspeedTypeLayout? layout)
    {
        foreach (MaxspeedTypeLayout recognizedLayout in recognizedLayouts!)
        {
            if (Regex.IsMatch(key, recognizedLayout.RegexPattern))
            {
                layout = recognizedLayout;
                return true;
            }
        }

        layout = null;
        return false;
    }

    private record MaxspeedTypeLayout(string TagLabel, string RegexPattern);

    private abstract record TaggedElement(OsmElement Element, string Key, string Value);
    private record RecognizedTaggedElement(OsmElement Element, string Key, string Value, MaxspeedTypeLayout Layout) : TaggedElement(Element, Key, Value);
    private record UnrecognizedTaggedElement(OsmElement Element, string Key, string Value) : TaggedElement(Element, Key, Value);

    [Pure]
    private static string TypeToLabel(OsmElement.OsmElementType type)
    {
        return type switch
        {
            OsmElement.OsmElementType.Node => "node",
            OsmElement.OsmElementType.Way => "way",
            OsmElement.OsmElementType.Relation => "relation",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    [Pure]
    private static MaxspeedTypeVariantMatch? MatchMaxspeedTypeValue(string key, string value, MaxspeedTypeVariantDefinition[] recognized)
    {
        // Advisory can only be from sign
        if (key == "maxspeed:type:advisory" && value != "sign")
            return null;
        
        foreach (MaxspeedTypeVariantDefinition definition in recognized)
        {
            Match match = Regex.Match(value, "^" + definition.Pattern + "$");
            
            if (match.Success)
            {
                if (match.Groups.Count > 1)
                    return new MaxspeedTypeVariantMatch(definition.Variant, int.Parse(match.Groups[1].Value));
                
                return new MaxspeedTypeVariantMatch(definition.Variant, null);
            }
        }

        return null;
    }

    private record MaxspeedTypeVariantDefinition(MaxspeedTypeVariant Variant, string Pattern, string DisplayLabel);
    
    private record MaxspeedTypeVariantMatch(MaxspeedTypeVariant Variant, int? Value);

    private enum MaxspeedTypeVariant
    {
        Sign,
        Urban,
        Rural,
        LivingStreet,
        Zone,
        Construction,
        ParkingLot,
        FuelStation
    }

    private enum ReportGroup
    {
        InvalidValues,
        MismatchedValues,
        MissingMaxspeed,
        InvalidMaxspeed,
        UnexpectedElements,
        UnrecognizedLayouts,
        Stats
    }
}