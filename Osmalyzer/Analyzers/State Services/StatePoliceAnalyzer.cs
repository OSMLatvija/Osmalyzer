namespace Osmalyzer;

[UsedImplicitly]
public class StatePoliceAnalyzer : Analyzer
{
    public override string Name => "State police offices";

    public override string Description => "This report checks that all state police offices listed on government's website are found on the map " +
                                          "and that they have the correct tags.";

    public override AnalyzerGroup Group => AnalyzerGroup.StateServices;

    public override List<Type> GetRequiredDataTypes() =>
    [
        typeof(LatviaOsmAnalysisData),
        typeof(StatePolicePoiAnalysisData),
        typeof(StatePoliceListAnalysisData)
    ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmData OsmData = osmData.MasterData;
                
        OsmData osmPoliceOffices = OsmData.Filter(
            new HasAnyValue("amenity", "police"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.FuzzyLoose) // a couple OOB hits
        );

        // Load police office data from both sources
        List<StatePoliceData> listedPoliceOffices = datas.OfType<StatePolicePoiAnalysisData>().First().Offices;
        List<StatePoliceListEntry> contactListEntries = datas.OfType<StatePoliceListAnalysisData>().First().Entries;

        // Match contact list entries to POI entries by name
        MatchListEntriesToPois(listedPoliceOffices, contactListEntries, out List<StatePoliceListEntry> unmatchedListEntries, out List<StatePoliceData> unmatchedPoiOffices);

        // Prepare data comparer/correlator
        Correlator<StatePoliceData> correlator = new Correlator<StatePoliceData>(
            osmPoliceOffices,
            listedPoliceOffices,
            new MatchDistanceParamater(100),
            new MatchFarDistanceParamater(200),
            new MatchExtraDistanceParamater(MatchStrength.Strong, 500),
            new DataItemLabelsParamater("State police office", "State police offices"),
            new OsmElementPreviewValue("name", true)
        );
        
        // Parse and report primary matching and location correlation
        CorrelatorReport correlatorReport = correlator.Parse(
            report,
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(true),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch()
        );
        
        // Validate
        
        Validator<StatePoliceData> validator = new Validator<StatePoliceData>(
            correlatorReport,
            "Tagging issues"
        );

        List<SuggestedAction> suggestedChanges = validator.Validate(
            report,
            false, false,
            new ValidateElementValueMatchesDataItemValue<StatePoliceData>("name", h => h.Name), // todo: can we shorten these meaningfully?
            new ValidateElementValueMatchesDataItemValue<StatePoliceData>("official_name", h => h.Name),
            new ValidateElementHasValue("operator", "Valsts policija"),
            new ValidateElementHasValue("operator:wikidata", "Q3741089"),
            new ValidateElementHasValue("operator:type", "government"),
            new ValidateElementHasValue("operator:website", "https://www.vp.gov.lv")
            // todo: new ValidateElementHasValue("police:LV", "state") -- as opposed to municipal
        );

#if DEBUG
        SuggestedActionApplicator.ApplyAndProposeXml(OsmData, suggestedChanges, this, "changes");
        SuggestedActionApplicator.ExplainForReport(suggestedChanges, report, ExtraReportGroup.ProposedChanges);
#endif
        
        // List all
        
        report.AddGroup(
            ExtraReportGroup.AllStations,
            "All Stations"
        );

        foreach (StatePoliceData policeOffice in listedPoliceOffices)
        {
            report.AddEntry(
                ExtraReportGroup.AllStations,
                new IssueReportEntry(
                    policeOffice.ReportString()
                )
            );
        }
        
        // List unmatched data items
        
        report.AddGroup(
            ExtraReportGroup.UnmatchedItems,
            "Unmatched items"
        );
        
        foreach (StatePoliceListEntry unmatchedListEntry in unmatchedListEntries)
        {
            report.AddEntry(
                ExtraReportGroup.UnmatchedItems,
                new IssueReportEntry(
                    "Could not match contact list entry to a map POI entry: " + unmatchedListEntry.ReportString()
                )
            );
        }

        foreach (StatePoliceData unmatchedPoiOffice in unmatchedPoiOffices)
        {
            report.AddEntry(
                ExtraReportGroup.UnmatchedItems,
                new IssueReportEntry(
                    "Could not match map POI entry to a contact list entry: " + unmatchedPoiOffice.ReportString()
                )
            );
        }
    }


    private static void MatchListEntriesToPois(
        List<StatePoliceData> poiOffices, List<StatePoliceListEntry> listEntries, 
        out List<StatePoliceListEntry> unmatchedListEntries,
        out List<StatePoliceData> unmatchedPoiOffices)
    {
        // Track unmatched, so we can report them later
        unmatchedListEntries = new List<StatePoliceListEntry>(listEntries);
        unmatchedPoiOffices = [ ];

        foreach (StatePoliceData poiEntry in poiOffices)
        {
            StatePoliceListEntry? listEntryMatch = FindMatchingListEntry(poiEntry, unmatchedListEntries);

            if (listEntryMatch != null)
            {
                poiEntry.SetListData(listEntryMatch);
                unmatchedListEntries.Remove(listEntryMatch);
            }
            else
            {
                unmatchedPoiOffices.Add(poiEntry);
            }
        }

        return;

        
        static StatePoliceListEntry? FindMatchingListEntry(StatePoliceData poi, List<StatePoliceListEntry> candidates)
        {
            // Exact name match (most entries on both lists share the same full name)
            foreach (StatePoliceListEntry listEntry in candidates)
            {
                string poiName = poi.Name;
                string listName = listEntry.Name;
                
                // Fix for:
                // List "Rīgas Pārdaugavas pārvalde"
                // Map  "Valsts policijas Rīgas reģiona pārvaldes Rīgas Pārdaugavas pārvalde"
                if (listName.StartsWith("Rīgas "))
                    listName = "Valsts policijas Rīgas reģiona pārvaldes " + listName;
                
                if (string.Equals(poiName, listName, StringComparison.InvariantCultureIgnoreCase))
                    return listEntry;
            }

            return null;
        }
    }

    
    private enum ExtraReportGroup
    {
        AllStations,
        ProposedChanges,
        UnmatchedItems
    }
}