using WikidataSharp;

namespace Osmalyzer;

[UsedImplicitly]
public class MunicipalityAnalyzer : Analyzer
{
    public override string Name => "Municipalities";

    public override string Description => 
        "This report checks that all municipalities are mapped.";

    public override AnalyzerGroup Group => AnalyzerGroup.Administrative;


    public override List<Type> GetRequiredDataTypes() => [ 
        typeof(LatviaOsmAnalysisData), 
        typeof(AddressGeodataAnalysisData),
        typeof(AtvkAnalysisData),
        typeof(MunicipalitiesWikidataData),
        typeof(StateCitiesAnalysisData),
        typeof(VdbAnalysisData)
    ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract osmMunicipalities = osmMasterData.Filter(
            new IsRelation(),
            new HasValue("boundary", "administrative"),
            new HasAnyValue("admin_level", "5"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.CentroidInside) // lots around edges
        );

        // Get municipality data

        AddressGeodataAnalysisData addressData = datas.OfType<AddressGeodataAnalysisData>().First();

        List<AtvkEntry> atvkEntries = datas.OfType<AtvkAnalysisData>().First().Entries
                                           .Where(e => !e.IsExpired && e.Designation == AtvkDesignation.Municipality).ToList();
        
        MunicipalitiesWikidataData wikidataData = datas.OfType<MunicipalitiesWikidataData>().First();
        
        StateCitiesAnalysisData stateCitiesData = datas.OfType<StateCitiesAnalysisData>().First();
        
        VdbAnalysisData vdbData = datas.OfType<VdbAnalysisData>().First();
        
        // Match VZD and ATVK data items

        Equivalator<Municipality, AtvkEntry> equivalator = new Equivalator<Municipality, AtvkEntry>(
            addressData.Municipalities, 
            atvkEntries
        );
        
        equivalator.MatchItems(
            (i1, i2) => i1.Name == i2.Name // we have no name conflicts in municipalities, so this is sufficient
        );
        
        Dictionary<Municipality, AtvkEntry> dataItemMatches = equivalator.AsDictionary();
        if (dataItemMatches.Count == 0) throw new Exception("No VZD-ATVK matches found for data items; data is probably broken.");
        
        // Assign WikiData
        
        wikidataData.Assign(
            addressData.Municipalities,
            (i, wd) => i.Name == wd.GetBestName("lv"), // we have no name conflicts in municipalities, so this is sufficient
            75000,
            out List<WikidataData.WikidataMatchIssue> wikidataMatchIssues
        );

        // Assign VDB data

        vdbData.AssignToDataItems(
            addressData.Municipalities,
            vdbData.Municipalities,
            i => i.Name,
            null,
            null,
            75000,
            out List<VdbMatchIssue> vdbMatchIssues
        );

        // Prepare data comparer/correlator

        Correlator<Municipality> municipalityCorrelator = new Correlator<Municipality>(
            osmMunicipalities,
            addressData.Municipalities,
            new MatchDistanceParamater(25000),
            new MatchFarDistanceParamater(75000),
            new MatchCallbackParameter<Municipality>(GetMunicipalityMatchStrength),
            new OsmElementPreviewValue("name", false),
            new DataItemLabelsParamater("municipality", "municipalities"),
            new LoneElementAllowanceParameter(DoesOsmElementLookLikeAMunicipality)
        );

        [Pure]
        MatchStrength GetMunicipalityMatchStrength(Municipality municipality, OsmElement osmElement)
        {
            string? name = osmElement.GetValue("name");

            if (name == municipality.Name)
                return MatchStrength.Strong; // exact match on name

            if (DoesOsmElementLookLikeAMunicipality(osmElement))
                return MatchStrength.Good; // looks like a municipality, but not exact match
            
            return MatchStrength.Unmatched;
        }

        [Pure]
        bool DoesOsmElementLookLikeAMunicipality(OsmElement element)
        {
            string? place = element.GetValue("place");
            if (place == "municipality")
                return true; // explicitly tagged
            
            if (place == "city")
                return false; // explicitly not tagged
            
            string? name = element.GetValue("name");
            if (name != null && stateCitiesData.StateCities.Any(sc => sc.Name == name))
                return false; // state cities have the same admin level as municipalities, but we know them, so we can exclude them
                        
            if (name != null && name.Contains("rajono"))
                return false; // Lithuanian district
            
            return true;
        }

        // Parse and report primary matching and location correlation

        CorrelatorReport municipalityCorrelation = municipalityCorrelator.Parse(
            report, 
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(true),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch()
        );
        
        // Validate municipality boundaries
        
        const double matchLimit = 0.99;
        
        report.AddGroup(
            ExtraReportGroup.MunicipalityBoundaries,
            "Municipality boundary issues",
            "This section lists municipalities where the mapped boundary does not sufficiently cover the official boundary polygon. " +
            "Due to data fuzziness, small mismatches are expected and not reported (" + (matchLimit * 100).ToString("F1") + "% coverage required)."
        );

        foreach (Correlation correlation in municipalityCorrelation.Correlations)
        {
            if (correlation is MatchedCorrelation<Municipality> matchedCorrelation)
            {
                Municipality municipality = matchedCorrelation.DataItem;
                OsmElement osmElement = matchedCorrelation.OsmElement;

                if (osmElement is OsmRelation relation)
                {
                    OsmMultiPolygon? relationMultiPolygon = relation.GetMultipolygon();
                    
                    if (relationMultiPolygon == null)
                    {
                        report.AddEntry(
                            ExtraReportGroup.MunicipalityBoundaries,
                            new IssueReportEntry(
                                "Municipality relation does not have a valid polygon for " + osmElement.OsmViewUrl,
                                osmElement.AverageCoord,
                                MapPointStyle.Problem,
                                osmElement
                            )
                        );
                        
                        continue;
                    }
                    
                    OsmMultiPolygon municipalityBoundary = municipality.Boundary!;

                    double estimatedCoverage = municipalityBoundary.GetOverlapCoveragePercent(relationMultiPolygon, 50);

                    if (estimatedCoverage < matchLimit)
                    {
                        report.AddEntry(
                            ExtraReportGroup.MunicipalityBoundaries,
                            new IssueReportEntry(
                                "Municipality boundary for `" + municipality.Name + "` does not match the official boundary polygon " +
                                "(matches at " + (estimatedCoverage * 100).ToString("F1") + "%) for " + osmElement.OsmViewUrl,
                                new SortEntryAsc(estimatedCoverage),
                                osmElement.AverageCoord,
                                estimatedCoverage < 0.95 ? MapPointStyle.Problem : MapPointStyle.Dubious,
                                osmElement
                            )
                        );
                    }
                }
            }
        }
        
        // Validate municipality syntax
        
        Validator<Municipality> municipalityValidator = new Validator<Municipality>(
            municipalityCorrelation,
            "Municipality syntax issues"
        );

        List<SuggestedAction> suggestedChanges = municipalityValidator.Validate(
            report,
            false, false,
            new ValidateElementHasValue("place", "municipality"),
            new ValidateElementHasValue("border_type", "municipality"),
            new ValidateElementValueMatchesDataItemValue<Municipality>("ref:LV:addr", m => m.AddressID, [ "ref" ]),
            new ValidateElementValueMatchesDataItemValue<Municipality>("ref", m => dataItemMatches.TryGetValue(m, out AtvkEntry? match) ? match.Code : null),
            new ValidateElementValueMatchesDataItemValue<Municipality>("ref:lau", m => dataItemMatches.TryGetValue(m, out AtvkEntry? match) ? match.Code : null),
            new ValidateElementValueMatchesDataItemValue<Municipality>("wikidata", m => m.WikidataItem?.QID)
        );

#if DEBUG
        SuggestedActionApplicator.ApplyAndProposeXml(osmMasterData, suggestedChanges, this);
        SuggestedActionApplicator.ExplainForReport(suggestedChanges, report, ExtraReportGroup.ProposedChanges);
#endif

        // List invalid municipalities that are still in data
        
        report.AddGroup(
            ExtraReportGroup.InvalidMunicipalities,
            "Invalid Municipalities",
            "Municipalities marked invalid in address geodata (not approved or not existing).",
            "There are no invalid municipalities in the geodata."
        );

        foreach (Municipality municipality in addressData.InvalidMunicipalities)
        {
            report.AddEntry(
                ExtraReportGroup.InvalidMunicipalities,
                new IssueReportEntry(
                    municipality.ReportString()
                )
            );
        }
        
        // Check that Wikidata values match OSM values
        
        // TODO:
        // TODO:
        // TODO:
        
        // List extra data items from non-OSM that were not matched
        
        report.AddGroup(
            ExtraReportGroup.ExternalDataMatchingIssues,
            "Extra data item matching issues",
            "This section lists any issues with data item matching to additional external data sources.",
            "No issues found."
        );
        
        List<AtvkEntry> extraAtvkEntries = atvkEntries
                                           .Where(e => !dataItemMatches.Values.Contains(e))
                                           .ToList();
        
        foreach (AtvkEntry atvkEntry in extraAtvkEntries)
        {
            report.AddEntry(
                ExtraReportGroup.ExternalDataMatchingIssues,
                new IssueReportEntry(
                    "ATVK entry for municipality `" + atvkEntry.Name + "` (#`" + atvkEntry.Code + "`) was not matched to any OSM element."
                )
            );
        }
        
        List<WikidataItem> extraWikidataItems = wikidataData.Municipalities
                                                            .Where(wd => addressData.Municipalities.All(c => c.WikidataItem != wd))
                                                            .ToList();

        foreach (WikidataItem wikidataItem in extraWikidataItems)
        {
            string? name = wikidataItem.GetBestName("lv") ?? null;

            report.AddEntry(
                ExtraReportGroup.ExternalDataMatchingIssues,
                new IssueReportEntry(
                    "Wikidata municipality item " + wikidataItem.WikidataUrl + (name != null ? " `" + name + "` " : "") + " was not matched to any OSM element."
                )
            );
        }
        
        foreach (WikidataData.WikidataMatchIssue matchIssue in wikidataMatchIssues)
        {
            switch (matchIssue)
            {
                case WikidataData.MultipleWikidataMatchesWikidataMatchIssue<Municipality> multipleWikidataMatches:
                    report.AddEntry(
                        ExtraReportGroup.ExternalDataMatchingIssues,
                        new IssueReportEntry(
                            multipleWikidataMatches.DataItem.ReportString() + " matched multiple Wikidata items: " +
                            string.Join(", ", multipleWikidataMatches.WikidataItems.Select(wd => wd.WikidataUrl))
                        )
                    );
                    break;
                
                case WikidataData.CoordinateMismatchWikidataMatchIssue<Municipality> coordinateMismatch:
                    report.AddEntry(
                        ExtraReportGroup.ExternalDataMatchingIssues,
                        new IssueReportEntry(
                            coordinateMismatch.DataItem.ReportString() + " matched a Wikidata item, but the Wikidata coordinate is too far at " +
                            coordinateMismatch.DistanceMeters.ToString("F0") + " m" +
                            " -- " + coordinateMismatch.WikidataItem.WikidataUrl
                        )
                    );
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(matchIssue));
            }
        }

        foreach (VdbMatchIssue vdbMatchIssue in vdbMatchIssues)
        {
            switch (vdbMatchIssue)
            {
                case MultipleVdbMatchesVdbMatchIssue<Municipality> multipleVdbMatches:
                    report.AddEntry(
                        ExtraReportGroup.ExternalDataMatchingIssues,
                        new IssueReportEntry(
                            multipleVdbMatches.DataItem.ReportString() + " matched multiple VDB entries: " +
                            string.Join(", ", multipleVdbMatches.VdbEntries.Select(vdb => vdb.ReportString()))
                        )
                    );
                    break;
                
                case CoordinateMismatchVdbMatchIssue<Municipality> coordinateMismatch:
                    report.AddEntry(
                        ExtraReportGroup.ExternalDataMatchingIssues,
                        new IssueReportEntry(
                            coordinateMismatch.DataItem.ReportString() + " matched a VDB entry, but the VDB coordinate is too far at " +
                            coordinateMismatch.DistanceMeters.ToString("F0") + " m" +
                            " -- " + coordinateMismatch.VdbEntry.ReportString()
                        )
                    );
                    break;
                
                case PoorMatchVdbMatchIssue<Municipality> coordinateMismatch:
                    report.AddEntry(
                        ExtraReportGroup.ExternalDataMatchingIssues,
                        new GenericReportEntry(
                            coordinateMismatch.DataItem.ReportString() + " matched a VDB entry, but poorly as a fallback (and might be wrong)" +
                            " -- " + coordinateMismatch.VdbEntry.ReportString()
                        )
                    );
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(vdbMatchIssue));
            }
        }

        foreach (Municipality municipality in addressData.Municipalities)
        {
            if (municipality.WikidataItem == null)
            {
                report.AddEntry(
                    ExtraReportGroup.ExternalDataMatchingIssues,
                    new IssueReportEntry(
                        municipality.ReportString() + " does not have a matched Wikidata item."
                    )
                );
            }
            
            if (municipality.VdbEntry == null)
            {
                List<VdbEntry> potentials = vdbData.Municipalities.Where(e => e.Name == municipality.Name).ToList();

                report.AddEntry(
                    ExtraReportGroup.ExternalDataMatchingIssues,
                    new IssueReportEntry(
                        municipality.ReportString() + " does not have a matched VDB entry." +
                        (potentials.Count > 0 ? " Potential matches: " + string.Join(", ", potentials.Select(p => p.ReportString())) : "")
                    )
                );
            }
        }
    }


    private enum ExtraReportGroup
    {
        MunicipalityBoundaries,
        InvalidMunicipalities,
        ExternalDataMatchingIssues,
        ProposedChanges
    }
}
