namespace Osmalyzer;

[UsedImplicitly]
public class VillageAnalyzer : Analyzer
{
    public override string Name => "Villages";

    public override string Description => 
        "This report checks that all villages (and hamlets) are mapped. " +
        "The village VAR data is pretty much complete, however hamlet data is much less so.";

    public override AnalyzerGroup Group => AnalyzerGroup.Miscellaneous;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData), typeof(AddressGeodataAnalysisData) ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract osmVillages = osmMasterData.Filter(
            new OrMatch(
                // Either village node
                new AndMatch(
                    new IsNode(),
                    new HasAnyValue("place", "village")
                ),
                // Or village boundary
                new AndMatch(
                    new IsRelation(),
                    new HasValue("boundary", "administrative"),
                    new HasAnyValue("admin_level", "9")
                )
            ),
            new DoesntHaveKey("EHAK:code"), // some Estonian villages still leak over the border, but they seem to have import values we can use
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.Fuzzy) // lots around edges
        );

        osmVillages = osmVillages.Deduplicate(AreMatchingVillageDefiners);
        
        OsmDataExtract osmHamlets = osmMasterData.Filter(
            new IsNode(),
            new HasAnyValue("place", "hamlet"),
            new DoesntHaveKey("EHAK:code"), // some Estonian villages still leak over the border, but they seem to have import values we can use
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.Fuzzy) // lots around edges
        );
        
        [Pure]
        static OsmElement? AreMatchingVillageDefiners(OsmElement element1, OsmElement element2)
        {
            // When we have a boundary and an admin center node, consider them duplicates and keep the boundary only

            if (element1 is OsmNode node1 && element2 is OsmRelation relation2)
                return AreMatching(node1, relation2) ? node1 : null;

            if (element1 is OsmRelation relation1 && element2 is OsmNode node2)
                return AreMatching(node2, relation1) ? node2 : null;
            
            return null;

            
            [Pure]
            static bool AreMatching(OsmNode osmNode, OsmRelation osmRelation)
            {
                // Strict check - is the node an (admin center) member of the relation?
                return osmRelation.Members.Any(m => m.Element == osmNode);
                // todo: looser check so we can detect issues but still assume they are the same village?
            }
        }

        // Get village/hamlet data

        AddressGeodataAnalysisData adddressData = datas.OfType<AddressGeodataAnalysisData>().First();

        // Parse villages
        
        // Prepare data comparer/correlator

        Correlator<Village> villageCorrelator = new Correlator<Village>(
            osmVillages,
            adddressData.Villages.Where(v => !v.IsHamlet && v.Valid).ToList(),
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
            ExtraReportGroup.VillageCorrelator,
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
                        village.Address +
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
        
        report.AddGroup(
            ExtraReportGroup.VillageBoundaries,
            "Village boundary issues"
        );

        foreach (Correlation correlation in villageCorrelation.Correlations)
        {
            if (correlation is MatchedCorrelation<Village> matchedCorrelation)
            {
                Village village = matchedCorrelation.DataItem;
                OsmElement osmElement = matchedCorrelation.OsmElement;

                if (osmElement is OsmRelation relation)
                {
                    OsmPolygon relationPloygon = relation.GetOuterWayPolygon();
                    OsmPolygon villageBoundary = village.Boundary!;

                    float coversBoundary = villageBoundary.GetOverlapCoveragePercent(relationPloygon);
                    
                    if (coversBoundary < 0.9f)
                    {
                        report.AddEntry(
                            ExtraReportGroup.VillageBoundaries,
                            new IssueReportEntry(
                                "Village boundary for `" + village.Name + "` does not cover the official boundary area " +
                                "(matches at " + (coversBoundary * 100).ToString("F1") + "%)",
                                village.Coord,
                                MapPointStyle.Problem
                            )
                        );
                    }
                }
            }
        }
        
        // Parse hamlets
        
        // Prepare data comparer/correlator

        Correlator<Village> hamletCorrelator = new Correlator<Village>(
            osmHamlets,
            adddressData.Villages.Where(v => v.IsHamlet && v.Valid).ToList(),
            new MatchDistanceParamater(100), // nodes should have good distance matches since data isnt polygons
            new MatchFarDistanceParamater(2000),
            new MatchCallbackParameter<Village>(GetHamletMatchStrength),
            new OsmElementPreviewValue("name", false),
            new DataItemLabelsParamater("hamlet", "hamlets"),
            new LoneElementAllowanceParameter(DoesOsmElementLookLikeAHamlet)
        );

        [Pure]
        MatchStrength GetHamletMatchStrength(Village village, OsmElement osmElement)
        {
            string? name = osmElement.GetValue("name");

            if (name == village.Name)
                return MatchStrength.Strong; // exact match on name

            if (DoesOsmElementLookLikeAVillage(osmElement))
                return MatchStrength.Good; // looks like a village, but not exact match
            
            return MatchStrength.Unmatched;
        }

        [Pure]
        bool DoesOsmElementLookLikeAHamlet(OsmElement element)
        {
            string? place = element.GetValue("place");
            if (place == "hamlet")
                return true; // explicitly tagged
            
            string? name = element.GetValue("name");
            if (name?.EndsWith("apkaime") == true)
                return false; // e.g. Riga suburb
            
            return true;
        }

        // Parse and report primary matching and location correlation

        CorrelatorReport hamletCorrelation = hamletCorrelator.Parse(
            report, 
            ExtraReportGroup.HamletCorrelator,
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(true),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch()
        );
        
        // Offer syntax for quick OSM addition for unmatched hamlets
        
        List<Village> unmatchedHamlets = hamletCorrelation.Correlations
            .OfType<UnmatchedItemCorrelation<Village>>()
            .Select(c => c.DataItem)
            .ToList();

        if (unmatchedHamlets.Count > 0)
        {
            report.AddGroup(
                ExtraReportGroup.SuggestedHamletAdditions,
                "Suggested Hamlet Additions",
                "These hamlets are not currently matched to OSM and can be added with these (suggested) tags."
            );

            foreach (Village hamlet in unmatchedHamlets)
            {
                string tagsBlock = BuildSuggestedVillageTags(hamlet);

                report.AddEntry(
                    ExtraReportGroup.SuggestedHamletAdditions,
                    new IssueReportEntry(
                        '`' + hamlet.Name + "` hamlet at `" +
                        hamlet.Address +
                        "` can be added at " +
                        hamlet.Coord.OsmUrl +
                        " as" + Environment.NewLine + tagsBlock,
                        hamlet.Coord,
                        MapPointStyle.Suggestion
                    )
                );
            }
        }
        
        // Validate additional issues

        // todo:
        
        // List invalid villages that are still in data
        
        // Create a group and dump all invalid village entries from geodata for awareness/tracking
        report.AddGroup(
            ExtraReportGroup.InvalidVillages,
            "Invalid Villages",
            "Villages and hamlets marked invalid in address geodata (not approved or not existing).",
            "There are no invalid villages or hamlets in the geodata."
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
            "place=" + (village.IsHamlet ? "hamlet" : "village"),
            "ref=" + village.ID
        ];

        return "```" + string.Join(Environment.NewLine, lines) + "```";
    }


    private enum ExtraReportGroup
    {
        VillageCorrelator,
        SuggestedVillageAdditions,
        VillageBoundaries,
        HamletCorrelator,
        SuggestedHamletAdditions,
        InvalidVillages
    }
}