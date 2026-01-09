using System.Diagnostics;
using WikidataSharp;

namespace Osmalyzer;

[UsedImplicitly]
public class HamletAnalyzer : AdminAnalyzerBase<Hamlet>
{
    public override string Name => "Hamlets";

    public override string Description => 
        "This report checks that all hamlets are mapped.";

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
        
        OsmDataExtract osmHamlets = osmMasterData.Filter(
            new IsNode(),
            new HasAnyValue("place", "hamlet"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.CentroidInside) // lots around edges
        );
        
        Console.WriteLine("OSM data filtered (" + stopwatch.ElapsedMilliseconds + " ms)");

        // Get hamlet data

        AddressGeodataAnalysisData addressData = datas.OfType<AddressGeodataAnalysisData>().First();

        VillagesWikidataData villagesWikidataData = datas.OfType<VillagesWikidataData>().First();
        
        ParishesWikidataData parishesWikidataData = datas.OfType<ParishesWikidataData>().First();
        
        VdbAnalysisData vdbData = datas.OfType<VdbAnalysisData>().First();
        
        // Assign VDB data

        stopwatch.Restart();
        
        vdbData.AssignToDataItems(
            addressData.Hamlets,
            vdbData.Hamlets,
            i => i.Name,
            i => i.ParishName,
            i => i.MunicipalityName,
            10000,
            out List<VdbMatchIssue> vdbMatchIssues
        );
        
        Console.WriteLine("VDB data assigned (" + stopwatch.ElapsedMilliseconds + " ms)");
        
        // Assign WikiData

        stopwatch.Restart();
        
        villagesWikidataData.AssignVillageOrHamlet( // todo: specific once wikidata is fixed
            addressData.Hamlets,
            (i, wd) =>
                i.Name == wd.GetBestName("lv") &&
                //(addressData.IsUniqueHamletName(i.Name) || // if the name is unique, it cannot conflict, so we don't need to check hierarchy
                i.ParishName == GetWikidataAdminItemOwnerName(wd),//)
                // we cannot assume wikidata is correct to rely on unique names and it has lots of hamlet mistagging, so their list includes non-hamlets too
            10000,
            out List<WikidataData.WikidataMatchIssue> wikidataMatchIssues
        );
        
        string? GetWikidataAdminItemOwnerName(WikidataItem wikidataItem)
        {
            long? ownerValue = wikidataItem.GetBestStatementValueAsQID(WikiDataProperty.LocatedInAdministrativeTerritorialEntity);
            if (ownerValue == null)
                return null;
            
            WikidataItem? ownerItem = parishesWikidataData.Parishes.FirstOrDefault(w => w.ID == ownerValue);
            if (ownerItem == null)
                return null;

            string? ownerName = ownerItem.GetBestName("lv");
            
            //Console.WriteLine($"Parish Wikidata item {wikidataItem.QID} owner municipality: {ownerName} ({ownerItem.QID})");
            
            return ownerName;
        }
        
        Console.WriteLine("Wikidata assigned (" + stopwatch.ElapsedMilliseconds + " ms)");

        // Parse hamlets
        
        // Prepare data comparer/correlator

        Correlator<Hamlet> hamletCorrelator = new Correlator<Hamlet>(
            osmHamlets,
            addressData.Hamlets,
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
            string? refAddr = osmElement.GetValue("ref:LV:addr");
            
            if (refAddr == hamlet.AddressID)
                return MatchStrength.Strong; // exact match on address id (presumably previously-imported and assumed correct)

            string? name = osmElement.GetValue("name");

            if (name == hamlet.Name)
                return MatchStrength.Strong; // exact match on name
            
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

        stopwatch.Restart();
        
        CorrelatorReport hamletCorrelation = hamletCorrelator.Parse(
            report, 
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(true),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch()
        );
        
        Console.WriteLine("Hamlets correlated (" + stopwatch.ElapsedMilliseconds + " ms)");
        
        // Offer syntax for quick OSM addition for unmatched hamlets
        
        stopwatch.Restart();
        
        List<Hamlet> unmatchedHamlets = hamletCorrelation.Correlations
            .OfType<UnmatchedItemCorrelation<Hamlet>>()
            .Select(c => c.DataItem)
            .ToList();
        
        Console.WriteLine("Unmatched hamlets identified (" + stopwatch.ElapsedMilliseconds + " ms)");

        if (unmatchedHamlets.Count > 0)
        {
            report.AddGroup(
                ExtraReportGroup.SuggestedHamletAdditions,
                "Suggested Hamlet Additions",
                "These hamlets are not currently matched to OSM and can potentially be added based on the source data items."
            );
            
#if DEBUG
            OsmData additionsData = osmMasterData.Copy();
            List<SuggestedAction> suggestedAdditions = [ ];
#endif

            stopwatch.Restart();
            
            OsmDataExtract namedNodes = osmMasterData.Filter(
                new HasKey("name"),
                // Manually filter out known non-hamlet things that have the same names
                new DoesntHaveKey("ref:LV:addr"), // we know this is a real separate feature, even if the name matches 
                new DoesntHaveAnyValue("public_transport", "platform", "stop_position", "stop_area"),
                new DoesntHaveKey("waterway"),
                new DoesntHaveValue("type", "waterway"),
                new DoesntHaveValue("highway", "bus_stop"),
                new DoesntHaveValue("historic", "manor"),
                new DoesntHaveValue("place", "village"), // we check all villages separately, so it's definitely not that 
                new DoesntHaveValue("place", "isolated_dwelling"),  // while it may actually be mistagging, it's very unlikely, but at the same time lots of hamlets and stuff are named after local name, which also often matches isolated dwellings
                new DoesntHaveValue("place", "suburb"),
                new DoesntHaveValue("place", "neighbourhood"),
                new DoesntHaveValue("railway", "station"),
                new DoesntHaveValue("historic:railway", "station"),
                new DoesntHaveValue("abandoned:railway", "station"),
                new DoesntHaveValue("landuse", "military"),
                new DoesntHaveKey("traffic_sign"),
                new DoesntHaveKey("power"),
                new DoesntHaveKey("advertising")
            );

            foreach (Hamlet hamlet in unmatchedHamlets)
            {
                const double newElementConflictDistance = 30000; // km
                List<OsmElement> closestElements = namedNodes.GetClosestElementsTo(hamlet.Coord, newElementConflictDistance);
                List<OsmElement> matchingNamed = closestElements.Where(e => e.GetValue("name") == hamlet.Name).ToList();

                if (matchingNamed.Count > 0)
                {
                    report.AddEntry(
                        ExtraReportGroup.SuggestedHamletAdditions,
                        new IssueReportEntry(
                            hamlet.ReportString() + " could be added at " + hamlet.Coord.OsmUrl + ", but there are nearby OSM element(s) with matching `name`, so it possibly already exists, but is mistagged: " +
                            string.Join(", ", matchingNamed.Select(e => e.OsmViewUrl)),
                            hamlet.Coord,
                            MapPointStyle.Dubious
                        )
                    );
                    
                    continue;
                }
                
#if DEBUG
                OsmNode newHamletNode = additionsData.CreateNewNode(hamlet.Coord);
                // todo: just set values directly instead of this, I only needed this for validator, which doesn't edit data directly
                suggestedAdditions.Add(new OsmCreateElementAction(newHamletNode));
                suggestedAdditions.Add(new OsmSetValueSuggestedAction(newHamletNode, "name", hamlet.Name));
                suggestedAdditions.Add(new OsmSetValueSuggestedAction(newHamletNode, "place", "hamlet"));
                suggestedAdditions.Add(new OsmSetValueSuggestedAction(newHamletNode, "ref:LV:addr", hamlet.AddressID));
                suggestedAdditions.Add(new OsmSetValueSuggestedAction(newHamletNode, "designation", "mazciems"));
#endif

                report.AddEntry(
                    ExtraReportGroup.SuggestedHamletAdditions,
                    new IssueReportEntry(
                        '`' + hamlet.Name + "` hamlet at " + hamlet.ReportString() + " can be added at " + hamlet.Coord.OsmUrl,
                        hamlet.Coord,
                        MapPointStyle.Suggestion
                    )
                );
            }
            
            Console.WriteLine("Suggested hamlet additions processed (" + stopwatch.ElapsedMilliseconds + " ms)");
            
#if DEBUG
            stopwatch.Restart();
            SuggestedActionApplicator.ApplyAndProposeXml(additionsData, suggestedAdditions, this, "additions");
            Console.WriteLine("Suggested additions applied (" + stopwatch.ElapsedMilliseconds + " ms)");
            SuggestedActionApplicator.ExplainForReport(suggestedAdditions, report, ExtraReportGroup.SuggestedHamletAdditions);
#endif
        }
        
        // Validate hamlet syntax
        
        Validator<Hamlet> hamletValidator = new Validator<Hamlet>(
            hamletCorrelation,
            "Hamlet syntax issues"
        );

        stopwatch.Restart();
        
        List<SuggestedAction> suggestedChanges = hamletValidator.Validate(
            report,
            false, false,
            new ValidateElementValueMatchesDataItemValue<Hamlet>("name", h => h.Name),
            new ValidateElementHasValue("place", "hamlet"),
            new ValidateElementValueMatchesDataItemValue<Hamlet>("ref:LV:addr", h => h.AddressID, [ "ref" ]),
            new ValidateElementValueMatchesDataItemValue<Hamlet>("wikidata", h => h.WikidataItem?.QID),
            //new ValidateElementValueMatchesDataItemValue<Hamlet>("ref:LV:VDB", h => h.VdbEntry?.ID.ToString()),
            new ValidateElementHasValue("designation", "mazciems")
        );
        
        Console.WriteLine("Hamlet syntax validated (" + stopwatch.ElapsedMilliseconds + " ms)");

#if DEBUG
        stopwatch.Restart();
        SuggestedActionApplicator.ApplyAndProposeXml(osmMasterData, suggestedChanges, this, "changes");
        Console.WriteLine("Suggested actions applied (" + stopwatch.ElapsedMilliseconds + " ms)");
        SuggestedActionApplicator.ExplainForReport(suggestedChanges, report, ExtraReportGroup.ProposedChanges);
#endif
        
        // List invalid hamlets that are still in data
        
        report.AddGroup(
            ExtraReportGroup.InvalidHamlets,
            "Invalid Hamlets",
            "Hamlets marked invalid in address geodata (not approved or not existing).",
            "There are no invalid hamlets in the geodata."
        );

        foreach (Hamlet hamlet in addressData.InvalidHamlets)
        {
            report.AddEntry(
                ExtraReportGroup.InvalidHamlets,
                new IssueReportEntry(
                    hamlet.ReportString()
                )
            );
        }
        
        // List extra data items from non-OSM that were not matched
        
        AddExternalDataMatchingIssuesGroup(report, ExtraReportGroup.ExternalDataMatchingIssues);
        
        stopwatch.Restart();
        
        // ReportExtraWikidataItems(report, ExtraReportGroup.ExternalDataMatchingIssues, villagesWikidataData.Hamlets, addressData.Hamlets, "hamlet");
        ReportWikidataMatchIssues(report, ExtraReportGroup.ExternalDataMatchingIssues, wikidataMatchIssues);
        
        Console.WriteLine("Wikidata match issues reported (" + stopwatch.ElapsedMilliseconds + " ms)");
        
        stopwatch.Restart();
        
        ReportVdbMatchIssues(report, ExtraReportGroup.ExternalDataMatchingIssues, vdbMatchIssues);
        
        Console.WriteLine("VDB match issues reported (" + stopwatch.ElapsedMilliseconds + " ms)");
        
        stopwatch.Restart();
        
        ReportMissingWikidataItems(report, ExtraReportGroup.ExternalDataMatchingIssues, addressData.Hamlets);
        
        Console.WriteLine("Missing Wikidata items reported (" + stopwatch.ElapsedMilliseconds + " ms)");
        
        stopwatch.Restart();
        
        ReportMissingVdbEntries(report, ExtraReportGroup.ExternalDataMatchingIssues, addressData.Hamlets, vdbData.Hamlets);
        
        Console.WriteLine("Missing VDB entries reported (" + stopwatch.ElapsedMilliseconds + " ms)");
        
        stopwatch.Restart();
        
        ReportUnmatchedOsmWikidataValues(report, ExtraReportGroup.ExternalDataMatchingIssues, addressData.Hamlets, hamletCorrelation);
        
        Console.WriteLine("Unmatched OSM Wikidata values reported (" + stopwatch.ElapsedMilliseconds + " ms)");
    }


    private enum ExtraReportGroup
    {
        SuggestedHamletAdditions,
        InvalidHamlets,
        ExternalDataMatchingIssues,
        ProposedChanges
    }
}
