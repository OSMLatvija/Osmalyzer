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
            new HasAnyValue("admin_level", "5"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.CentroidInside) // lots around edges
        );

        // Get municipality data

        AddressGeodataAnalysisData adddressData = datas.OfType<AddressGeodataAnalysisData>().First();

        // Prepare data comparer/correlator

        Correlator<Municipality> municipalityCorrelator = new Correlator<Municipality>(
            osmMunicipalities,
            adddressData.Municipalities.Where(m => m.Valid).ToList(),
            new MatchDistanceParamater(25000),
            new MatchFarDistanceParamater(60000),
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
            if (name is "Daugavpils" or "Jelgava" or "Jēkabpils" or "Jūrmala" or "Liepāja" or "Ogre" or "Rēzekne" or "Rīga" or "Valmiera" or "Ventspils")
                return false; // cities have the same admin level, but we know them
                        
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
        
        // Validate municipality syntax
        
        Validator<Municipality> municipalityValidator = new Validator<Municipality>(
            municipalityCorrelation,
            "Municipality syntax issues"
        );

        municipalityValidator.Validate(
            report,
            false,
            new ValidateElementValueMatchesDataItemValue<Municipality>("ref", m => m.ID)
        );
        
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
        MunicipalityBoundaries,
        InvalidMunicipalities
    }
}

