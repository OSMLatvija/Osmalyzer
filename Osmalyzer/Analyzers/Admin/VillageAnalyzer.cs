namespace Osmalyzer;

[UsedImplicitly]
public class VillageAnalyzer : Analyzer
{
    public override string Name => "Villages";

    public override string Description => 
        "This report checks that all villages are mapped. " +
        "The village VAR data is pretty much complete.";

    public override AnalyzerGroup Group => AnalyzerGroup.Administrative;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData), typeof(AddressGeodataAnalysisData) ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract osmVillages = osmMasterData.Filter(
            new IsRelation(),
            new HasValue("boundary", "administrative"),
            new HasAnyValue("admin_level", "9"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.CentroidInside) // lots around edges
        );

        // Get village/hamlet data

        AddressGeodataAnalysisData adddressData = datas.OfType<AddressGeodataAnalysisData>().First();

        // Parse villages
        
        // Prepare data comparer/correlator

        Correlator<Village> villageCorrelator = new Correlator<Village>(
            osmVillages,
            adddressData.Villages.Where(v => v.Valid).ToList(),
            new MatchDistanceParamater(500), // todo: lower distance, but allow match inside relation
            new MatchFarDistanceParamater(2000),
            new MatchCallbackParameter<Village>(GetVillageMatchStrength),
            new OsmElementPreviewValue("name", false),
            new DataItemLabelsParamater("village", "villages"),
            new LoneElementAllowanceParameter(DoesOsmElementLookLikeAVillage)
        );

        [Pure]
        MatchStrength GetVillageMatchStrength(Village village, OsmElement osmElement)
        {
            string? name = osmElement.GetValue("name");

            if (name == village.Name)
                return MatchStrength.Strong; // exact match on name

            if (DoesOsmElementLookLikeAVillage(osmElement))
                return MatchStrength.Good; // looks like a village, but not exact match
            
            return MatchStrength.Unmatched;
        }

        [Pure]
        bool DoesOsmElementLookLikeAVillage(OsmElement element)
        {
            if (element.HasKey("EHAK:code")) // Estonian villages leaking over border
                return false;
            
            string? place = element.GetValue("place");
            if (place == "village")
                return true; // explicitly tagged
            
            string? name = element.GetValue("name");
            if (name?.EndsWith("apkaime") == true)
                return false; // e.g. Riga suburb
            
            return true;
        }

        // Parse and report primary matching and location correlation

        CorrelatorReport villageCorrelation = villageCorrelator.Parse(
            report, 
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(true),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch()
        );
        
        // Offer syntax for quick OSM addition for unmatched villages
        
        List<Village> unmatchedVillages = villageCorrelation.Correlations
            .OfType<UnmatchedItemCorrelation<Village>>()
            .Select(c => c.DataItem)
            .ToList();

        if (unmatchedVillages.Count > 0)
        {
            report.AddGroup(
                ExtraReportGroup.SuggestedVillageAdditions,
                "Suggested Village Additions",
                "These villages are not currently matched to OSM and can be added with these (suggested) tags."
            );

            foreach (Village village in unmatchedVillages)
            {
                string tagsBlock = BuildSuggestedVillageTags(village);

                report.AddEntry(
                    ExtraReportGroup.SuggestedVillageAdditions,
                    new IssueReportEntry(
                        '`' + village.Name + "` village at `" +
                        village.ReportString() +
                        "` can be added at " +
                        village.Coord.OsmUrl +
                        " as" + Environment.NewLine + tagsBlock,
                        village.Coord,
                        MapPointStyle.Suggestion
                    )
                );
            }
        }
        
        // Validate village boundaries
        
        const double matchLimit = 0.99;
        
        report.AddGroup(
            ExtraReportGroup.VillageBoundaries,
            "Village boundary issues",
            "This section lists villages where the mapped boundary does not sufficiently cover the official boundary area (assuming village boundary is mapped and valid). " +
            "Due to data fuzziness, small mismatches are expected and not reported (" + (matchLimit * 100).ToString("F1") + "% coverage required)."
        );

        foreach (Correlation correlation in villageCorrelation.Correlations)
        {
            if (correlation is MatchedCorrelation<Village> matchedCorrelation)
            {
                Village village = matchedCorrelation.DataItem;
                OsmElement osmElement = matchedCorrelation.OsmElement;

                if (osmElement is OsmRelation relation)
                {
                    OsmMultiPolygon? relationMultiPolygon = relation.GetMultipolygon();
                    
                    if (relationMultiPolygon == null)
                    {
                        report.AddEntry(
                            ExtraReportGroup.VillageBoundaries,
                            new IssueReportEntry(
                                "Village relation for `" + village.Name + "` does not have a valid polygon for " + osmElement.OsmViewUrl,
                                osmElement.AverageCoord,
                                MapPointStyle.Problem,
                                osmElement
                            )
                        );
                        
                        continue;
                    }
                    
                    OsmMultiPolygon villageBoundary = village.Boundary!;

                    double estimatedCoverage = villageBoundary.GetOverlapCoveragePercent(relationMultiPolygon, 20);

                    if (estimatedCoverage < matchLimit)
                    {
                        report.AddEntry(
                            ExtraReportGroup.VillageBoundaries,
                            new IssueReportEntry(
                                "Village boundary for `" + village.Name + "` does not match the official boundary area " +
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
        
        // Validate village syntax
        
        Validator<Village> villageValidator = new Validator<Village>(
            villageCorrelation,
            "Village syntax issues"
        );

        List<SuggestedAction> suggestedChanges = villageValidator.Validate(
            report,
            false,
            new ValidateElementValueMatchesDataItemValue<Village>("ref", v => v.ID)
        );

#if DEBUG
        SuggestedActionApplicator.ApplyAndProposeXml(osmMasterData, suggestedChanges, this);
#endif
        
        // List invalid villages that are still in data
        
        // Create a group and dump all invalid village entries from geodata for awareness/tracking
        report.AddGroup(
            ExtraReportGroup.InvalidVillages,
            "Invalid Villages",
            "Villages marked invalid in address geodata (not approved or not existing).",
            "There are no invalid villages in the geodata."
        );

        List<Village> invalidVillages = adddressData.Villages.Where(v => !v.Valid).ToList();

        foreach (Village village in invalidVillages)
        {
            report.AddEntry(
                ExtraReportGroup.InvalidVillages,
                new IssueReportEntry(
                    village.ReportString()
                )
            );
        }
    }


    [Pure]
    private static string BuildSuggestedVillageTags(Village village)
    {
        List<string> lines =
        [
            "name=" + village.Name,
            "place=village",
            "ref=" + village.ID
        ];

        return "```" + string.Join(Environment.NewLine, lines) + "```";
    }


    private enum ExtraReportGroup
    {
        SuggestedVillageAdditions,
        VillageBoundaries,
        InvalidVillages
    }
}