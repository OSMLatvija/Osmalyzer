using WikidataSharp;

namespace Osmalyzer;

[UsedImplicitly]
public class HamletAnalyzer : Analyzer
{
    public override string Name => "Hamlets";

    public override string Description => 
        "This report checks that all hamlets are mapped. " +
        "Hamlet data is much less complete compared to villages.";

    public override AnalyzerGroup Group => AnalyzerGroup.Administrative;


    public override List<Type> GetRequiredDataTypes() => [ 
        typeof(LatviaOsmAnalysisData), 
        typeof(AddressGeodataAnalysisData),
        typeof(VillagesWikidataData)
    ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract osmHamlets = osmMasterData.Filter(
            new IsNode(),
            new HasAnyValue("place", "hamlet"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.CentroidInside) // lots around edges
        );

        // Get hamlet data

        AddressGeodataAnalysisData addressData = datas.OfType<AddressGeodataAnalysisData>().First();

        VillagesWikidataData wikidataData = datas.OfType<VillagesWikidataData>().First();

        // Parse hamlets
        
        // Prepare data comparer/correlator

        Correlator<Hamlet> hamletCorrelator = new Correlator<Hamlet>(
            osmHamlets,
            addressData.Hamlets.Where(h => h.Valid).ToList(),
            new MatchDistanceParamater(100), // nodes should have good distance matches since data isnt polygons
            new MatchFarDistanceParamater(2000),
            new MatchCallbackParameter<Hamlet>(GetHamletMatchStrength),
            new OsmElementPreviewValue("name", false),
            new DataItemLabelsParamater("hamlet", "hamlets"),
            new LoneElementAllowanceParameter(DoesOsmElementLookLikeAHamlet)
        );

        [Pure]
        MatchStrength GetHamletMatchStrength(Hamlet hamlet, OsmElement osmElement)
        {
            string? name = osmElement.GetValue("name");

            if (name == hamlet.Name)
                return MatchStrength.Strong; // exact match on name

            if (DoesOsmElementLookLikeAHamlet(osmElement))
                return MatchStrength.Good; // looks like a hamlet, but not exact match
            
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
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(true),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch()
        );
        
        // Offer syntax for quick OSM addition for unmatched hamlets
        
        List<Hamlet> unmatchedHamlets = hamletCorrelation.Correlations
            .OfType<UnmatchedItemCorrelation<Hamlet>>()
            .Select(c => c.DataItem)
            .ToList();

        if (unmatchedHamlets.Count > 0)
        {
            report.AddGroup(
                ExtraReportGroup.SuggestedHamletAdditions,
                "Suggested Hamlet Additions",
                "These hamlets are not currently matched to OSM and can be added with these (suggested) tags."
            );

            foreach (Hamlet hamlet in unmatchedHamlets)
            {
                string tagsBlock = BuildSuggestedHamletTags(hamlet);

                report.AddEntry(
                    ExtraReportGroup.SuggestedHamletAdditions,
                    new IssueReportEntry(
                        '`' + hamlet.Name + "` hamlet at " +
                        hamlet.ReportString() +
                        " can be added at " +
                        hamlet.Coord.OsmUrl +
                        " as" + Environment.NewLine + tagsBlock,
                        hamlet.Coord,
                        MapPointStyle.Suggestion
                    )
                );
            }
        }
        
        // Validate hamlet syntax
        
        Validator<Hamlet> hamletValidator = new Validator<Hamlet>(
            hamletCorrelation,
            "Hamlet syntax issues"
        );

        List<SuggestedAction> suggestedChanges = hamletValidator.Validate(
            report,
            false,
            new ValidateElementHasValue("place", "hamlet"),
            new ValidateElementValueMatchesDataItemValue<Hamlet>("ref:LV:addr", h => h.AddressID, [ "ref" ]),
            new ValidateElementValueMatchesDataItemValue<Hamlet>("wikidata", h => h.WikidataItem?.QID)
        );

#if DEBUG
        SuggestedActionApplicator.ApplyAndProposeXml(osmMasterData, suggestedChanges, this);
#endif
        
        // List invalid hamlets that are still in data
        
        // Create a group and dump all invalid hamlet entries from geodata for awareness/tracking
        report.AddGroup(
            ExtraReportGroup.InvalidHamlets,
            "Invalid Hamlets",
            "Hamlets marked invalid in address geodata (not approved or not existing).",
            "There are no invalid hamlets in the geodata."
        );

        List<Hamlet> invalidHamlets = addressData.Hamlets.Where(h => !h.Valid).ToList();

        foreach (Hamlet hamlet in invalidHamlets)
        {
            report.AddEntry(
                ExtraReportGroup.InvalidHamlets,
                new IssueReportEntry(
                    hamlet.ReportString()
                )
            );
        }
        
        // List extra data items from non-OSM that were not matched
        
        // report.AddGroup(
        //     ExtraReportGroup.ExtraDataItems,
        //     "Extra data items",
        //     "This section lists data items from additional external data sources that were not matched to any OSM element.",
        //     "All external data items were matched to OSM elements."
        // );
        
        // TODO: WE CAN'T TELL APART HAMLETS FROM VILLAGES IN WIKIDATA
        // List<WikidataItem> extraWikidataItems = wikidataData.Villages
        //                                                     .Where(wd => addressData.Cities.All(c => c.WikidataItem != wd))
        //                                                     .ToList();
        //
        // foreach (WikidataItem wikidataItem in extraWikidataItems)
        // {
        //     string? name = AdminWikidataData.GetBestName(wikidataItem, "lv") ?? null;
        //
        //     report.AddEntry(
        //         ExtraReportGroup.ExtraDataItems,
        //         new IssueReportEntry(
        //             "Wikidata village item " + wikidataItem.WikidataUrl + (name != null ? "`" + name + "` " : "") + " was not matched to any OSM element."
        //         )
        //     );
        // }
    }


    [Pure]
    private static string BuildSuggestedHamletTags(Hamlet hamlet)
    {
        List<string> lines =
        [
            "name=" + hamlet.Name,
            "place=hamlet",
            "ref:LV:addr=" + hamlet.AddressID
        ];

        return "```" + string.Join(Environment.NewLine, lines) + "```";
    }


    private enum ExtraReportGroup
    {
        SuggestedHamletAdditions,
        InvalidHamlets,
        ExtraDataItems
    }
}

