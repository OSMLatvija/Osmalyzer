namespace Osmalyzer;

[UsedImplicitly]
public class ParishAnalyzer : Analyzer
{
    public override string Name => "Parishes";

    public override string Description => 
        "This report checks that all parishes are mapped.";

    public override AnalyzerGroup Group => AnalyzerGroup.Administrative;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData), typeof(AddressGeodataAnalysisData) ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract osmParishes = osmMasterData.Filter(
            new IsRelation(),
            new HasValue("boundary", "administrative"),
            new HasAnyValue("admin_level", "8"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.Fuzzy) // lots around edges
        );

        // Get parish data

        AddressGeodataAnalysisData adddressData = datas.OfType<AddressGeodataAnalysisData>().First();

        // Prepare data comparer/correlator

        Correlator<Parish> parishCorrelator = new Correlator<Parish>(
            osmParishes,
            adddressData.Parishes.Where(p => p.Valid).ToList(),
            new MatchDistanceParamater(500),
            new MatchFarDistanceParamater(2000),
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
            string? boundary = element.GetValue("boundary");
            string? adminLevel = element.GetValue("admin_level");
            
            if (boundary == "administrative" && adminLevel == "8")
                return true; // explicitly tagged
            
            return true;
        }

        // Parse and report primary matching and location correlation

        CorrelatorReport parishCorrelation = parishCorrelator.Parse(
            report, 
            ExtraReportGroup.ParishCorrelator,
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
            "This section lists parishes where the mapped boundary does not sufficiently cover the official boundary area. " +
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
                    OsmPolygon relationPloygon = relation.GetOuterWayPolygon();
                    OsmPolygon parishBoundary = parish.Boundary!;

                    double estimatedCoverage = parishBoundary.GetOverlapCoveragePercent(relationPloygon);

                    if (estimatedCoverage < matchLimit)
                    {
                        report.AddEntry(
                            ExtraReportGroup.ParishBoundaries,
                            new IssueReportEntry(
                                "Parish boundary for `" + parish.Name + "` does not match the official boundary area " +
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
        
        // List invalid parishes that are still in data
        
        report.AddGroup(
            ExtraReportGroup.InvalidParishes,
            "Invalid Parishes",
            "Parishes marked invalid in address geodata (not approved or not existing).",
            "There are no invalid parishes in the geodata."
        );

        List<Parish> invalidParishes = adddressData.Parishes.Where(p => !p.Valid).ToList();

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
        ParishCorrelator,
        ParishBoundaries,
        InvalidParishes
    }
}
