namespace Osmalyzer;

[UsedImplicitly]
public class MunicipalityAnalyzer : Analyzer
{
    public override string Name => "Municipalities";

    public override string Description => 
        "This report checks that all municipalities are mapped.";

    public override AnalyzerGroup Group => AnalyzerGroup.Administrative;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData), typeof(AddressGeodataAnalysisData) ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract osmMunicipalities = osmMasterData.Filter(
            new IsRelation(),
            new HasValue("boundary", "administrative"),
            new HasAnyValue("admin_level", "7"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.Fuzzy) // lots around edges
        );

        // Get municipality data

        AddressGeodataAnalysisData adddressData = datas.OfType<AddressGeodataAnalysisData>().First();

        // Prepare data comparer/correlator

        Correlator<Municipality> municipalityCorrelator = new Correlator<Municipality>(
            osmMunicipalities,
            adddressData.Municipalities.Where(m => m.Valid).ToList(),
            new MatchDistanceParamater(1000),
            new MatchFarDistanceParamater(5000),
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
            string? boundary = element.GetValue("boundary");
            string? adminLevel = element.GetValue("admin_level");
            
            if (boundary == "administrative" && adminLevel == "7")
                return true; // explicitly tagged
            
            return true;
        }

        // Parse and report primary matching and location correlation

        CorrelatorReport municipalityCorrelation = municipalityCorrelator.Parse(
            report, 
            ExtraReportGroup.MunicipalityCorrelator,
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
            "This section lists municipalities where the mapped boundary does not sufficiently cover the official boundary area. " +
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
                    OsmPolygon relationPloygon = relation.GetOuterWayPolygon();
                    OsmPolygon municipalityBoundary = municipality.Boundary!;

                    double estimatedCoverage = municipalityBoundary.GetOverlapCoveragePercent(relationPloygon);

                    if (estimatedCoverage < matchLimit)
                    {
                        report.AddEntry(
                            ExtraReportGroup.MunicipalityBoundaries,
                            new IssueReportEntry(
                                "Municipality boundary for `" + municipality.Name + "` does not match the official boundary area " +
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
        
        // List invalid municipalities that are still in data
        
        report.AddGroup(
            ExtraReportGroup.InvalidMunicipalities,
            "Invalid Municipalities",
            "Municipalities marked invalid in address geodata (not approved or not existing).",
            "There are no invalid municipalities in the geodata."
        );

        List<Municipality> invalidMunicipalities = adddressData.Municipalities.Where(m => !m.Valid).ToList();

        foreach (Municipality municipality in invalidMunicipalities)
        {
            report.AddEntry(
                ExtraReportGroup.InvalidMunicipalities,
                new IssueReportEntry(
                    municipality.ReportString()
                )
            );
        }
    }


    private enum ExtraReportGroup
    {
        MunicipalityCorrelator,
        MunicipalityBoundaries,
        InvalidMunicipalities
    }
}

