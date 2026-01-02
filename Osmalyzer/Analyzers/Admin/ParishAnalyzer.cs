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
        typeof(ParishesWikidataData)
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

        List<AtkvEntry> atvkEntries = datas.OfType<AtvkAnalysisData>().First().Entries
                                           .Where(e => !e.IsExpired && e.Designation == AtkvDesignation.Parish).ToList();

        ParishesWikidataData wikidataData = datas.OfType<ParishesWikidataData>().First();

        // Prepare data comparer/correlator

        Correlator<Parish> parishCorrelator = new Correlator<Parish>(
            osmParishes,
            addressData.Parishes.Where(p => p.Valid).ToList(),
            new MatchDistanceParamater(10000),
            new MatchFarDistanceParamater(30000),
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
        
        // Match VZD and ATVK data items

        Equivalator<Parish, AtkvEntry> equivalator = new Equivalator<Parish, AtkvEntry>(
            addressData.Parishes, 
            atvkEntries
        );
        
        equivalator.MatchItems(
            (i1, i2) => i1.Name == i2.Name && i1.MunicipalityName == i2.Parent?.Name // there are repeat parish names, specifically "Pilskalnes pagasts" and "Salas pagasts"
        );
        
        Dictionary<Parish, AtkvEntry> dataItemMatches = equivalator.AsDictionary();
        if (dataItemMatches.Count == 0) throw new Exception("No VZD-ATVK matches found for data items; data is probably broken.");
        
        // Assign WikiData
        
        wikidataData.Assign(
            addressData.Parishes,
            i => i.Name,
            (i, wd) => i.WikidataItem = wd
        );
        
        // Validate parish syntax
        
        Validator<Parish> parishValidator = new Validator<Parish>(
            parishCorrelation,
            "Parish syntax issues"
        );

        List<SuggestedAction> suggestedChanges = parishValidator.Validate(
            report,
            false,
            new ValidateElementHasValue("place", "civil_parish"), // not "parish"
            new ValidateElementValueMatchesDataItemValue<Parish>("ref:LV:addr", p => p.AddressID, [ "ref" ]),
            new ValidateElementValueMatchesDataItemValue<Parish>("ref", p => dataItemMatches.TryGetValue(p, out AtkvEntry? match) ? match.Code : null),
            new ValidateElementValueMatchesDataItemValue<Parish>("wikidata", p => p.WikidataItem?.QID)
        );

#if DEBUG
        SuggestedActionApplicator.ApplyAndProposeXml(osmMasterData, suggestedChanges, this);
#endif
        
        // List invalid parishes that are still in data
        
        report.AddGroup(
            ExtraReportGroup.InvalidParishes,
            "Invalid Parishes",
            "Parishes marked invalid in address geodata (not approved or not existing).",
            "There are no invalid parishes in the geodata."
        );

        List<Parish> invalidParishes = addressData.Parishes.Where(p => !p.Valid).ToList();

        foreach (Parish parish in invalidParishes)
        {
            report.AddEntry(
                ExtraReportGroup.InvalidParishes,
                new IssueReportEntry(
                    parish.ReportString()
                )
            );
        }
    }


    private enum ExtraReportGroup
    {
        ParishBoundaries,
        InvalidParishes
    }
}
