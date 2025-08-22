namespace Osmalyzer;

[UsedImplicitly]
public class MaxspeedTypeAnalyzer : Analyzer
{
    public override string Name => "Maxspeed Types";

    public override string Description => "The report checks validity of `maxspeed:type` values.";

    public override AnalyzerGroup Group => AnalyzerGroup.Roads;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData) ];

        
    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;
        
        OsmDataExtract elementsWithMaxspeedType = osmMasterData.Filter(
            new HasKey("maxspeed:type"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.Fuzzy)
        );

        // Parse
        
        // Check that maxspeed:type is only on ways and not on nodes or relations
        
        OsmDataExtract nonWaysWithMaxspeedType = elementsWithMaxspeedType.Filter(new IsNodeOrRelation());

        report.AddGroup(
            ReportGroup.UnexpectedElements, 
            "Elements where maxspeed:type is not expected"
        );

        foreach (OsmElement osmElement in nonWaysWithMaxspeedType.Elements)
        {
            report.AddEntry(
                ReportGroup.UnexpectedElements,
                new IssueReportEntry(
                    "This " + TypeToLabel(osmElement.ElementType) + " has a `maxspeed:type=" + osmElement.GetValue("maxspeed:type") + "`, but this tag is only expected on ways (roads). " + osmElement.OsmViewUrl,
                    osmElement.AverageCoord,
                    MapPointStyle.Problem
                )
            );
        }
        
        // Check that maxspeed:type is only on highways
        
        OsmDataExtract waysWithMaxspeedType = elementsWithMaxspeedType.Filter(new IsWay());
        
        OsmDataExtract nonHighwaysWithMaxspeedType = waysWithMaxspeedType.Filter(new DoesntHaveKey("highway"));
        
        foreach (OsmElement osmElement in nonHighwaysWithMaxspeedType.Elements)
        {
            report.AddEntry(
                ReportGroup.UnexpectedElements,
                new IssueReportEntry(
                    "This way has a `maxspeed:type=" + osmElement.GetValue("maxspeed:type") + "`, but this tag is only expected on `highway`s (roads). " + osmElement.OsmViewUrl,
                    osmElement.AverageCoord,
                    MapPointStyle.Problem
                )
            );
        }
        
        // Now check actual maxspeed:type values
        
        OsmDataExtract highwaysWithMaxspeedType = waysWithMaxspeedType.Filter(new HasKey("highway"));
        
        MaxspeedTypeVariantDefinition[] validMaxspeedTypes = [ 
            new MaxspeedTypeVariantDefinition(MaxspeedTypeVariant.Sign, "sign", "sign"),
            new MaxspeedTypeVariantDefinition(MaxspeedTypeVariant.Urban, "LV:urban", "LV:urban"),
            new MaxspeedTypeVariantDefinition(MaxspeedTypeVariant.Rural, "LV:rural", "LV:rural"),
            new MaxspeedTypeVariantDefinition(MaxspeedTypeVariant.LivingStreet, "LV:living_street", "LV:living_street"),
            new MaxspeedTypeVariantDefinition(MaxspeedTypeVariant.Zone, "LV:zone([0-9]{1,3})", "LV:zone##"),
            new MaxspeedTypeVariantDefinition(MaxspeedTypeVariant.Construction, "construction", "construction")
        ];
        
        report.AddGroup(
            ReportGroup.InvalidValues, 
            "Highways with invalid (unrecognized) maxspeed:type values.",
            "Valid values are: " + string.Join(", ", validMaxspeedTypes.Select(v => "`" + v.DisplayLabel + "`")) + "."
        );
        
        report.AddGroup(
            ReportGroup.MismatchedValues, 
            "Elements where maxspeed:type does not match maxspeed value"
        );
        
        report.AddGroup(
            ReportGroup.MissingMaxspeed, 
            "Elements where maxspeed:type is set but maxspeed is missing"
        );
        
        report.AddGroup(
            ReportGroup.InvalidMaxspeed, 
            "Elements where maxspeed:type is set but maxspeed is invalid/unrecognized"
        );
        
        foreach (OsmElement osmElement in highwaysWithMaxspeedType.Elements)
        {
            string maxspeedTypeValue = osmElement.GetValue("maxspeed:type")!;

            MaxspeedTypeVariantMatch? match = MatchMaxspeedTypeValue(maxspeedTypeValue, validMaxspeedTypes);
            
            if (match == null)
            {
                // Not valid/recognized, report
                
                report.AddEntry(
                    ReportGroup.InvalidValues,
                    new IssueReportEntry(
                        "This highway has an unrecognized `maxspeed:type=" + maxspeedTypeValue + "`. " + osmElement.OsmViewUrl,
                        osmElement.AverageCoord,
                        MapPointStyle.Problem
                    )
                );
            }
            else
            {
                // Valid; now check against maxspeed value
                
                string? maxspeedValue = osmElement.GetValue("maxspeed");

                if (maxspeedValue != null)
                {
                    if (int.TryParse(maxspeedValue, out int maxspeed))
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
                                            "This highway has a `maxspeed:type=" + maxspeedTypeValue + "`, but the corresponding `maxspeed=" + maxspeedValue + "` value does not match. Expected `maxspeed=50`. " + osmElement.OsmViewUrl,
                                            osmElement.AverageCoord,
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
                                            "This highway has a `maxspeed:type=" + maxspeedTypeValue + "`, but the corresponding `maxspeed=" + maxspeedValue + "` value does not match. Expected `maxspeed=90` or `80`. " + osmElement.OsmViewUrl,
                                            osmElement.AverageCoord,
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
                                            "This highway has a `maxspeed:type=" + maxspeedTypeValue + "`, but the corresponding `maxspeed=" + maxspeedValue + "` value does not match. Expected `maxspeed=20`. " + osmElement.OsmViewUrl,
                                            osmElement.AverageCoord,
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
                                            "This highway has a `maxspeed:type=" + maxspeedTypeValue + "`, but the corresponding `maxspeed=" + maxspeedValue + "` value does not match. Expected `maxspeed=" + match.Value.Value + "`. " + osmElement.OsmViewUrl,
                                            osmElement.AverageCoord,
                                            MapPointStyle.Problem
                                        )
                                    );
                                }
                                break;
                            
                            case MaxspeedTypeVariant.Construction:
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
                                "This highway has a `maxspeed:type=" + maxspeedTypeValue + "`, but the corresponding `maxspeed=" + maxspeedValue + "` value is invalid. " + osmElement.OsmViewUrl,
                                osmElement.AverageCoord,
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
                            "This highway has a `maxspeed:type=" + maxspeedTypeValue + "`, but is missing the corresponding `maxspeed` value. " + osmElement.OsmViewUrl,
                            osmElement.AverageCoord,
                            MapPointStyle.Problem
                        )
                    );
                }
            }
        }
    }


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
    private static MaxspeedTypeVariantMatch? MatchMaxspeedTypeValue(string value, MaxspeedTypeVariantDefinition[] recognized)
    {
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
        Construction
    }

    private enum ReportGroup
    {
        InvalidValues,
        MismatchedValues,
        MissingMaxspeed,
        InvalidMaxspeed,
        UnexpectedElements
    }
}