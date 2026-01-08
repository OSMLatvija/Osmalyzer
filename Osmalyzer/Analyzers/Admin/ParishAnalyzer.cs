using WikidataSharp;

namespace Osmalyzer;

[UsedImplicitly]
public class ParishAnalyzer : Analyzer
{
    public override string Name => "Parishes";

    public override string Description => 
        "This report checks that all parishes are mapped.";

    public override AnalyzerGroup Group => AnalyzerGroup.Administrative;


    public override List<Type> GetRequiredDataTypes() => [ 
        typeof(LatviaOsmAnalysisData), 
        typeof(AddressGeodataAnalysisData),
        typeof(AtvkAnalysisData),
        typeof(ParishesWikidataData),
        typeof(MunicipalitiesWikidataData),
        typeof(VdbAnalysisData)
    ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract osmParishes = osmMasterData.Filter(
            new IsRelation(),
            new HasValue("boundary", "administrative"),
            new HasAnyValue("admin_level", "8"),
            new OrMatch(
                new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.CentroidInside), // lots around edges
                new CustomMatch(e => e.GetValue("name")?.Contains("pagasts") == true) // due to border shape some centroids escape
            )
        );

        // Get parish data

        AddressGeodataAnalysisData addressData = datas.OfType<AddressGeodataAnalysisData>().First();

        List<AtvkEntry> atvkEntries = datas.OfType<AtvkAnalysisData>().First().Entries
                                           .Where(e => !e.IsExpired && e.Designation == AtvkDesignation.Parish).ToList();

        ParishesWikidataData wikidataData = datas.OfType<ParishesWikidataData>().First();
        
        MunicipalitiesWikidataData municipalitiesWikidataData = datas.OfType<MunicipalitiesWikidataData>().First();
        
        VdbAnalysisData vdbData = datas.OfType<VdbAnalysisData>().First();
        
        // Match VZD and ATVK data items

        Equivalator<Parish, AtvkEntry> equivalator = new Equivalator<Parish, AtvkEntry>(
            addressData.Parishes, 
            atvkEntries
        );
        
        equivalator.MatchItems(
            (i1, i2) => i1.Name == i2.Name && i1.MunicipalityName == i2.Parent?.Name // there are repeat parish names, specifically "Pilskalnes pagasts" and "Salas pagasts"
        );
        
        Dictionary<Parish, AtvkEntry> dataItemMatches = equivalator.AsDictionary();
        if (dataItemMatches.Count == 0) throw new Exception("No VZD-ATVK matches found for data items; data is probably broken.");
        
        // Assign WikiData

        wikidataData.Assign(
            addressData.Parishes,
            (i, wd) =>
                i.Name == wd.GetBestName("lv") &&
                (addressData.IsUniqueParishName(i.Name) || // if the name is unique, it cannot conflict, so we don't need to check hierarchy
                 i.MunicipalityName == GetWikidataAdminItemOwnerName(wd)),
            50000,
            out List<WikidataData.WikidataMatchIssue> wikidataMatchIssues
        );
        
        string? GetWikidataAdminItemOwnerName(WikidataItem wikidataItem)
        {
            long? ownerValue = wikidataItem.GetBestStatementValueAsQID(WikiDataProperty.LocatedInAdministrativeTerritorialEntity);
            if (ownerValue == null)
                return null;
            
            WikidataItem? ownerItem = municipalitiesWikidataData.Municipalities.FirstOrDefault(w => w.ID == ownerValue);
            if (ownerItem == null)
                return null;

            string? ownerName = ownerItem.GetBestName("lv");
            
            //Console.WriteLine($"Parish Wikidata item {wikidataItem.QID} owner municipality: {ownerName} ({ownerItem.QID})");
            
            return ownerName;
        }

        // Assign VDB data

        vdbData.AssignToDataItems(
            addressData.Parishes,
            (i, vdb) =>
                i.Name == vdb.Name &&
                vdb.ObjectType == VdbEntryObjectType.Parish &&
                i.MunicipalityName == vdb.Location1,
            50000,
            out List<VdbMatchIssue> vdbMatchIssues
        );

        // Prepare data comparer/correlator

        Correlator<Parish> parishCorrelator = new Correlator<Parish>(
            osmParishes,
            addressData.Parishes,
            new MatchDistanceParamater(10000),
            new MatchFarDistanceParamater(50000),
            new MatchCallbackParameter<Parish>(GetParishMatchStrength),
            new OsmElementPreviewValue("name", false),
            new DataItemLabelsParamater("parish", "parishes"),
            new LoneElementAllowanceParameter(DoesOsmElementLookLikeAParish)
        );

        [Pure]
        MatchStrength GetParishMatchStrength(Parish parish, OsmElement osmElement)
        {
            string? name = osmElement.GetValue("name");

            if (name == parish.Name)
                return MatchStrength.Strong; // exact match on name

            if (DoesOsmElementLookLikeAParish(osmElement))
                return MatchStrength.Good; // looks like a parish, but not exact match
            
            return MatchStrength.Unmatched;
        }

        [Pure]
        bool DoesOsmElementLookLikeAParish(OsmElement element)
        {
            string? place = element.GetValue("place");
            if (place == "parish")
                return true; // explicitly tagged
            
            string? name = element.GetValue("name");
            
            if (name != null && name.Contains("Савет"))
                return false; // Belarusian parishes leaking over
            
            if (name != null && name.Contains("поселение"))
                return false; // Russian town thing leaking over
            
            return true;
        }

        // Parse and report primary matching and location correlation

        CorrelatorReport parishCorrelation = parishCorrelator.Parse(
            report, 
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(true),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch()
        );
        
        // Validate parish boundaries
        
        const double matchLimit = 0.99;
        
        report.AddGroup(
            ExtraReportGroup.ParishBoundaries,
            "Parish boundary issues",
            "This section lists parishes where the mapped boundary does not sufficiently cover the official boundary polygon. " +
            "Due to data fuzziness, small mismatches are expected and not reported (" + (matchLimit * 100).ToString("F1") + "% coverage required)."
        );

        foreach (Correlation correlation in parishCorrelation.Correlations)
        {
            if (correlation is MatchedCorrelation<Parish> matchedCorrelation)
            {
                Parish parish = matchedCorrelation.DataItem;
                OsmElement osmElement = matchedCorrelation.OsmElement;

                if (osmElement is OsmRelation relation)
                {
                    OsmMultiPolygon? relationMultiPolygon = relation.GetMultipolygon();
                    
                    if (relationMultiPolygon == null)
                    {
                        report.AddEntry(
                            ExtraReportGroup.ParishBoundaries,
                            new IssueReportEntry(
                                "Parish relation for `" + parish.Name + "` does not have a valid polygon for " + osmElement.OsmViewUrl,
                                osmElement.AverageCoord,
                                MapPointStyle.Problem,
                                osmElement
                            )
                        );
                        
                        continue;
                    }
                    
                    OsmMultiPolygon parishBoundary = parish.Boundary!;

                    double estimatedCoverage = parishBoundary.GetOverlapCoveragePercent(relationMultiPolygon, 50);

                    if (estimatedCoverage < matchLimit)
                    {
                        report.AddEntry(
                            ExtraReportGroup.ParishBoundaries,
                            new IssueReportEntry(
                                "Parish boundary for `" + parish.Name + "` does not match the official boundary polygon " +
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
        
        // Validate parish syntax
        
        Validator<Parish> parishValidator = new Validator<Parish>(
            parishCorrelation,
            "Parish syntax issues"
        );

        List<SuggestedAction> suggestedChanges = parishValidator.Validate(
            report,
            false, false,
            new ValidateElementHasValue("place", "civil_parish"), // not "parish"
            new ValidateElementHasValue("border_type", "parish"), // not "civil_parish"
            new ValidateElementValueMatchesDataItemValue<Parish>("ref:LV:addr", p => p.AddressID, [ "ref" ]),
            new ValidateElementValueMatchesDataItemValue<Parish>("ref", p => dataItemMatches.TryGetValue(p, out AtvkEntry? match) ? match.Code : null),
            new ValidateElementValueMatchesDataItemValue<Parish>("wikidata", p => p.WikidataItem?.QID)
        );

#if DEBUG
        SuggestedActionApplicator.ApplyAndProposeXml(osmMasterData, suggestedChanges, this);
        SuggestedActionApplicator.ExplainForReport(suggestedChanges, report, ExtraReportGroup.ProposedChanges);
#endif
        
        // List invalid parishes that are still in data
        
        report.AddGroup(
            ExtraReportGroup.InvalidParishes,
            "Invalid Parishes",
            "Parishes marked invalid in address geodata (not approved or not existing).",
            "There are no invalid parishes in the geodata."
        );

        foreach (Parish parish in addressData.InvalidParishes)
        {
            report.AddEntry(
                ExtraReportGroup.InvalidParishes,
                new IssueReportEntry(
                    parish.ReportString()
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
                    "ATVK entry for parish `" + atvkEntry.Name + "` (#`" + atvkEntry.Code + "`) was not matched to any OSM element."
                )
            );
        }
        
        List<WikidataItem> extraWikidataItems = wikidataData.Parishes
                                                            .Where(wd => addressData.Parishes.All(c => c.WikidataItem != wd))
                                                            .ToList();

        foreach (WikidataItem wikidataItem in extraWikidataItems)
        {
            string? name = wikidataItem.GetBestName("lv") ?? null;

            report.AddEntry(
                ExtraReportGroup.ExternalDataMatchingIssues,
                new IssueReportEntry(
                    "Wikidata parish item " + wikidataItem.WikidataUrl + (name != null ? " `" + name + "` " : "") + " was not matched to any OSM element."
                )
            );
        }
        
        foreach (WikidataData.WikidataMatchIssue matchIssue in wikidataMatchIssues)
        {
            switch (matchIssue)
            {
                case WikidataData.MultipleWikidataMatchesWikidataMatchIssue<Parish> multipleWikidataMatches:
                    report.AddEntry(
                        ExtraReportGroup.ExternalDataMatchingIssues,
                        new IssueReportEntry(
                            multipleWikidataMatches.DataItem.ReportString() + " matched multiple Wikidata items: " +
                            string.Join(", ", multipleWikidataMatches.WikidataItems.Select(wd => wd.WikidataUrl))
                        )
                    );
                    break;
                
                case WikidataData.CoordinateMismatchWikidataMatchIssue<Parish> coordinateMismatch:
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
                case MultipleVdbMatchesVdbMatchIssue<Parish> multipleVdbMatches:
                    report.AddEntry(
                        ExtraReportGroup.ExternalDataMatchingIssues,
                        new IssueReportEntry(
                            multipleVdbMatches.DataItem.ReportString() + " matched multiple VDB entries: " +
                            string.Join(", ", multipleVdbMatches.VdbEntries.Select(vdb => vdb.ReportString()))
                        )
                    );
                    break;
                
                case CoordinateMismatchVdbMatchIssue<Parish> coordinateMismatch:
                    report.AddEntry(
                        ExtraReportGroup.ExternalDataMatchingIssues,
                        new IssueReportEntry(
                            coordinateMismatch.DataItem.ReportString() + " matched a VDB entry, but the VDB coordinate is too far at " +
                            coordinateMismatch.DistanceMeters.ToString("F0") + " m" +
                            " -- " + coordinateMismatch.VdbEntry.ReportString()
                        )
                    );
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(vdbMatchIssue));
            }
        }

        foreach (Parish parish in addressData.Parishes)
        {
            if (parish.WikidataItem == null)
            {
                report.AddEntry(
                    ExtraReportGroup.ExternalDataMatchingIssues,
                    new IssueReportEntry(
                        parish.ReportString() + " does not have a matched Wikidata item."
                    )
                );
            }
            
            if (parish.VdbEntry == null)
            {
                List<VdbEntry> potentials = vdbData.AdminEntries.Where(e => e.ObjectType == VdbEntryObjectType.Parish && e.Name == parish.Name).ToList();

                report.AddEntry(
                    ExtraReportGroup.ExternalDataMatchingIssues,
                    new IssueReportEntry(
                        parish.ReportString() + " does not have a matched VDB entry." +
                        (potentials.Count > 0 ? " Potential matches: " + string.Join(", ", potentials.Select(p => p.ReportString())) : "")
                    )
                );
            }
        }
    }


    private enum ExtraReportGroup
    {
        ParishBoundaries,
        InvalidParishes,
        ExternalDataMatchingIssues,
        ProposedChanges
    }
}
