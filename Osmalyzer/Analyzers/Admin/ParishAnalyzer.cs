using WikidataSharp;

namespace Osmalyzer;

[UsedImplicitly]
public class ParishAnalyzer : AdminAnalyzerBase<Parish>
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
        typeof(VdbAnalysisData),
        typeof(CspPopulationAnalysisData)
    ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmData OsmData = osmData.MasterData;

        OsmData osmParishes = OsmData.Filter(
            new IsRelation(),
            new HasValue("boundary", "administrative"),
            new HasAnyValue("admin_level", "8"),
            new OrMatch(
                new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.CentroidInside), // lots around edges
                new CustomMatch(e => e.GetValue("name")?.Contains("pagasts") == true) // due to border shape some centroids escape
            )
        );

        // Get all data sources

        AddressGeodataAnalysisData addressData = datas.OfType<AddressGeodataAnalysisData>().First();

        List<AtvkEntry> atvkEntries = datas.OfType<AtvkAnalysisData>().First().Entries
                                           .Where(e => !e.IsExpired && e.Designation == AtvkDesignation.Parish).ToList();

        ParishesWikidataData wikidataData = datas.OfType<ParishesWikidataData>().First();
        
        MunicipalitiesWikidataData municipalitiesWikidataData = datas.OfType<MunicipalitiesWikidataData>().First();
        
        VdbAnalysisData vdbData = datas.OfType<VdbAnalysisData>().First();
        
        CspPopulationAnalysisData cspData = datas.OfType<CspPopulationAnalysisData>().First();
        
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
            vdbData.Parishes,
            i => i.Name,
            i => i.MunicipalityName,
            null,
            50000,
            5000,
            out List<VdbMatchIssue> vdbMatchIssues
        );
        
        // Assign CSP population data
        
        cspData.AssignToDataItems(
            addressData.Cities,
            CspAreaType.Parish,
            i => i.Name,
            i => i.MunicipalityName // a couple of ambiguous ones need it
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
            string? refAddr = osmElement.GetValue("ref:LV:addr");
            
            if (refAddr == parish.AddressID)
                return MatchStrength.Strong; // exact match on address id (presumably previously-imported and assumed correct)

            string? name = osmElement.GetValue("name");

            if (name == parish.Name)
                return MatchStrength.Strong; // exact match on name
            
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
            new ValidateElementValueMatchesDataItemValue<Parish>("name", p => p.Name),
            new ValidateElementHasValue("place", "civil_parish"), // not "parish"
            new ValidateElementHasValue("border_type", "parish"), // not "civil_parish"
            new ValidateElementValueMatchesDataItemValue<Parish>("ref:LV:addr", p => p.AddressID, [ "ref" ]),
            new ValidateElementValueMatchesDataItemValue<Parish>("ref", p => dataItemMatches.TryGetValue(p, out AtvkEntry? match) ? match.Code : null),
            new ValidateElementValueMatchesDataItemValue<Parish>("wikidata", p => p.WikidataItem?.QID),
            new ValidateElementValueMatchesDataItemValue<Parish>("ref:LV:VDB", p => p.VdbEntry?.ID.ToString())
        );

#if DEBUG
        SuggestedActionApplicator.ApplyAndProposeXml(OsmData, suggestedChanges, this);
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
        
        // List extra data items from non-OSM that were not matched
        
        AddExternalDataMatchingIssuesGroup(report, ExtraReportGroup.ExternalDataMatchingIssues);
        
        ReportExtraAtvkEntries(report, ExtraReportGroup.ExternalDataMatchingIssues, atvkEntries, dataItemMatches, "parish");
        ReportExtraWikidataItems(report, ExtraReportGroup.ExternalDataMatchingIssues, wikidataData.Parishes, addressData.Parishes, "parish");
        ReportWikidataMatchIssues(report, ExtraReportGroup.ExternalDataMatchingIssues, wikidataMatchIssues);
        ReportVdbMatchIssues(report, ExtraReportGroup.ExternalDataMatchingIssues, vdbMatchIssues);
        ReportMissingWikidataItems(report, ExtraReportGroup.ExternalDataMatchingIssues, addressData.Parishes);
        ReportMissingVdbEntries(report, ExtraReportGroup.ExternalDataMatchingIssues, addressData.Parishes, vdbData.Parishes);
        ReportUnmatchedOsmWikidataValues(report, ExtraReportGroup.ExternalDataMatchingIssues, addressData.Parishes, parishCorrelation);
    }


    private enum ExtraReportGroup
    {
        ParishBoundaries,
        InvalidParishes,
        ExternalDataMatchingIssues,
        ProposedChanges
    }
}
