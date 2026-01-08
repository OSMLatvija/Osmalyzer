using System.Diagnostics;
using WikidataSharp;

namespace Osmalyzer;

[UsedImplicitly]
public class VillageAnalyzer : Analyzer
{
    public override string Name => "Villages";

    public override string Description => 
        "This report checks that all villages are mapped. " +
        "The village VAR data is pretty much complete (but hamlet data is not).";

    public override AnalyzerGroup Group => AnalyzerGroup.Administrative;


    public override List<Type> GetRequiredDataTypes() => [ 
        typeof(LatviaOsmAnalysisData), 
        typeof(AddressGeodataAnalysisData),
        typeof(VillagesWikidataData),
        typeof(ParishesWikidataData),
        typeof(VdbAnalysisData)
    ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        Console.WriteLine(); 
        
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmMasterData osmMasterData = osmData.MasterData;

        Stopwatch stopwatch = Stopwatch.StartNew();
        
        OsmDataExtract osmVillages = osmMasterData.Filter(
            new IsRelation(),
            new HasValue("boundary", "administrative"),
            new HasAnyValue("admin_level", "9"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.CentroidInside) // lots around edges
        );
        
        Console.WriteLine("OSM data filtered (" + stopwatch.ElapsedMilliseconds + " ms)");
        
        // Find preset centers of boundaries
        // (we won't need to set some values if there is the "master" node for the relation)

        stopwatch.Restart();
        
        foreach (OsmRelation relation in osmVillages.Relations)
        {
            List<OsmRelationMember> knownCenters = relation.Members.Where(m => m.Role == "admin_centre" && m.Element != null).ToList();

            if (knownCenters.Count == 1) // todo: else report
                relation.UserData = knownCenters[0].Element;

            if (knownCenters.Count == 0)
            {
                List<OsmRelationMember> labelCenters = relation.Members.Where(m => m.Role == "label" && m.Element != null).ToList();
                
                if (labelCenters.Count == 1)
                    relation.UserData = labelCenters[0].Element; // label is fine too
                // todo: do we need to check values like place= on it to make sure it's actually representing the center?
            }
        }

        Console.WriteLine("OSM admin centers identified (" + stopwatch.ElapsedMilliseconds + " ms)");
        
        // Get extra related data

        AddressGeodataAnalysisData addressData = datas.OfType<AddressGeodataAnalysisData>().First();

        VillagesWikidataData villagesWikidataData = datas.OfType<VillagesWikidataData>().First();
        
        ParishesWikidataData parishesWikidataData = datas.OfType<ParishesWikidataData>().First();
        
        VdbAnalysisData vdbData = datas.OfType<VdbAnalysisData>().First();
        
        // Assign VDB data

        stopwatch.Restart();
        
        vdbData.AssignToVillages(addressData.Villages);
        
        Console.WriteLine("VDB data assigned (" + stopwatch.ElapsedMilliseconds + " ms)");
        
        // Assign WikiData

        stopwatch.Restart();
        
        villagesWikidataData.AssignNonHamlets(
            addressData.Villages,
            (i, wd) =>
                i.Name == WikidataData.GetBestName(wd, "lv") &&
                //(addressData.IsUniqueVillageName(i.Name) || // if the name is unique, it cannot conflict, so we don't need to check hierarchy
                 i.ParishName == GetWikidataAdminItemOwnerName(wd),//)
                // todo: there is also Pilskalne in both same-named Pilskalne pagasts, so we need to check the owner of parish...
                // we cannot assume wikidata is correct to rely on unique names and it has lots of hamlet mistagging, so their list includes hamlets too
            out List<(Village, List<WikidataItem>)> multiMatches
        );
        
        string? GetWikidataAdminItemOwnerName(WikidataItem wikidataItem)
        {
            long? ownerValue = wikidataItem.GetStatementBestQIDValue(WikiDataProperty.LocatedInAdministrativeTerritorialEntity);
            if (ownerValue == null)
                return null;
            
            WikidataItem? ownerItem = parishesWikidataData.Parishes.FirstOrDefault(w => w.ID == ownerValue);
            if (ownerItem == null)
                return null;

            string? ownerName = WikidataData.GetBestName(ownerItem, "lv");
            
            //Console.WriteLine($"Parish Wikidata item {wikidataItem.QID} owner municipality: {ownerName} ({ownerItem.QID})");
            
            return ownerName;
        }
        
        Console.WriteLine("Wikidata assigned (" + stopwatch.ElapsedMilliseconds + " ms)"); // 2245 ms

        // Parse villages
        
        // Prepare data comparer/correlator
        
        Correlator<Village> villageCorrelator = new Correlator<Village>(
            osmVillages,
            addressData.Villages.Where(v => v.Valid).ToList(),
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

        stopwatch.Restart();
        
        CorrelatorReport villageCorrelation = villageCorrelator.Parse(
            report, 
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(true),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch()
        );
        
        Console.WriteLine("Villages correlated (" + stopwatch.ElapsedMilliseconds + " ms)");
        
        // Offer syntax for quick OSM addition for unmatched villages
        
        stopwatch.Restart();
        
        List<Village> unmatchedVillages = villageCorrelation.Correlations
            .OfType<UnmatchedItemCorrelation<Village>>()
            .Select(c => c.DataItem)
            .ToList();
        
        Console.WriteLine("Unmatched villages identified (" + stopwatch.ElapsedMilliseconds + " ms)");

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
                        '`' + village.Name + "` village at " +
                        village.ReportString() +
                        " can be added at " +
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
            "This section lists villages where the mapped boundary does not sufficiently cover the official boundary polygon (assuming village boundary is mapped and valid). " +
            "Due to data fuzziness, small mismatches are expected and not reported (" + (matchLimit * 100).ToString("F1") + "% coverage required)."
        );

        stopwatch.Restart();
        
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
                                "Village boundary for `" + village.Name + "` does not match the official boundary polygon " +
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
        
        Console.WriteLine("Village boundaries validated (" + stopwatch.ElapsedMilliseconds + " ms)"); // 8850 ms
        
        // Validate village syntax
        
        Validator<Village> villageValidator = new Validator<Village>(
            villageCorrelation,
            "Village syntax issues"
        );

        stopwatch.Restart();
        
        List<SuggestedAction> suggestedChanges = villageValidator.Validate(
            report,
            false, false,
            // On relation itself
            new ValidateElementHasValue("border_type", "village"),
            new ValidateElementValueMatchesDataItemValue<Village>("ref:LV:addr", v => v.AddressID, [ "ref" ]),
            // If no admin center given, check tags directly on relation
            new ValidateElementHasValue(e => e.UserData == null, "place", "village"),
            //todo:? new ValidateElementHasValue(e => e.UserData == null, "designation", "ciems"),
            //new ValidateElementDoesntHaveTag(e => e.UserData != null, "place"), -- these were imported for a LOT of them and are not really that wrong, so avoid removing them en masse
            new ValidateElementValueMatchesDataItemValue<Village>(e => e.UserData == null, "wikidata", c => c.WikidataItem?.QID),
            // If admin center given, check tags on the admin center node
            new ValidateElementHasValue(e => e.UserData != null, e => (OsmElement)e.UserData!, "place", "village"),
            //todo:? new ValidateElementHasValue(e => e.UserData != null, e => (OsmElement)e.UserData!, "designation", "ciems"),
            new ValidateElementValueMatchesDataItemValue<Village>(e => e.UserData != null, e => (OsmElement)e.UserData!, "wikidata", c => c.WikidataItem?.QID)
        );
        
        Console.WriteLine("Village syntax validated (" + stopwatch.ElapsedMilliseconds + " ms)");

#if DEBUG
        stopwatch = Stopwatch.StartNew();
        SuggestedActionApplicator.ApplyAndProposeXml(osmMasterData, suggestedChanges, this);
        Console.WriteLine("Suggested actions applied (" + stopwatch.ElapsedMilliseconds + " ms)");
        SuggestedActionApplicator.ExplainForReport(suggestedChanges, report, ExtraReportGroup.ProposedChanges);
#endif
        
        // List invalid villages that are still in data
        
        report.AddGroup(
            ExtraReportGroup.InvalidVillages,
            "Invalid Villages",
            "Villages marked invalid in address geodata (not approved or not existing).",
            "There are no invalid villages in the geodata."
        );

        List<Village> invalidVillages = addressData.Villages.Where(v => !v.Valid).ToList();

        foreach (Village village in invalidVillages)
        {
            report.AddEntry(
                ExtraReportGroup.InvalidVillages,
                new IssueReportEntry(
                    village.ReportString()
                )
            );
        }
        
        // Check that Wikidata values match OSM values
        
        // TODO:
        // TODO:
        // TODO:
        
        // List extra data items from non-OSM that were not matched
        
        report.AddGroup(
            ExtraReportGroup.ExtraDataItems,
            "Extra data items",
            "This section lists data items from additional external data sources that were not matched to any OSM element.",
            "All external data items were matched to OSM elements."
        );

        stopwatch.Restart();
        
        List<WikidataItem> extraWikidataItems = villagesWikidataData.NonHamlets
                                                                    .Where(wd => addressData.Villages.All(c => c.WikidataItem != wd))
                                                                    .ToList();
        
        Console.WriteLine("Extra WikiData items identified (" + stopwatch.ElapsedMilliseconds + " ms)");
        
        foreach (WikidataItem wikidataItem in extraWikidataItems)
        {
            string? name = WikidataData.GetBestName(wikidataItem, "lv") ?? null;
        
            report.AddEntry(
                ExtraReportGroup.ExtraDataItems,
                new IssueReportEntry(
                    "Wikidata village item " + wikidataItem.WikidataUrl + (name != null ? " `" + name + "` " : "") + " was not matched to any OSM element."
                )
            );
        }

        foreach ((Village village, List<WikidataItem> matches) in multiMatches)
        {
            report.AddEntry(
                ExtraReportGroup.ExtraDataItems,
                new IssueReportEntry(
                    village.ReportString() + " matched multiple Wikidata items: " +
                    string.Join(", ", matches.Select(wd => wd.WikidataUrl))
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
            "ref:LV:addr=" + village.AddressID
        ];

        return "```" + string.Join(Environment.NewLine, lines) + "```";
    }


    private enum ExtraReportGroup
    {
        SuggestedVillageAdditions,
        VillageBoundaries,
        InvalidVillages,
        ExtraDataItems,
        ProposedChanges
    }
}