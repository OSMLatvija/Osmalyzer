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
           
        OsmData OsmData = osmData.MasterData;

        Stopwatch stopwatch = Stopwatch.StartNew();
        
        OsmData osmHamlets = OsmData.Filter(
            new IsNode(),
            new HasAnyValue("place", "hamlet"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.CentroidInside) // lots around edges
        );
        
        Console.WriteLine("OSM data filtered (" + stopwatch.ElapsedMilliseconds + " ms)");

        // Get all data sources

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
            1000,
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
        
        // Validate hamlet syntax
        
        Validator<Hamlet> hamletValidator = new Validator<Hamlet>(
            hamletCorrelation,
            "Hamlet syntax issues"
        );

        stopwatch.Restart();

        List<ValidationRule> rules =
        [
            new ValidateElementValueMatchesDataItemValue<Hamlet>("name", h => h.Name),
            new ValidateElementHasValue("place", "hamlet"),
            new ValidateElementValueMatchesDataItemValue<Hamlet>("ref:LV:addr", h => h.AddressID, [ "ref" ]),
            new ValidateElementValueMatchesDataItemValue<Hamlet>("wikidata", h => h.WikidataItem?.QID),
            //new ValidateElementValueMatchesDataItemValue<Hamlet>("ref:LV:VDB", h => h.VdbEntry?.ID.ToString()),
            new ValidateElementHasValue("designation", "mazciems")
        ];
        
        Validation validation = hamletValidator.Validate(
            report,
            false, false,
            rules
        );
        
        Console.WriteLine("Hamlet syntax validated (" + stopwatch.ElapsedMilliseconds + " ms)");

#if DEBUG
        stopwatch.Restart();
        SuggestedActionApplicator.ApplyAndProposeXml(OsmData, validation.Changes, this, "changes");
        Console.WriteLine("Suggested actions applied (" + stopwatch.ElapsedMilliseconds + " ms)");
        SuggestedActionApplicator.ExplainForReport(validation.Changes, report, ExtraReportGroup.ProposedChanges);
#endif
        
        // Offer adding unmatched hamlets
        
        Spawner<NotaryOfficeData> spawner = new Spawner<NotaryOfficeData>(
            hamletCorrelation
        );
            
        Spawn spawn = spawner.Spawn(
            report,
            rules
        );

#if DEBUG
        OsmData additionsData = OsmData.Copy();
        SuggestedActionApplicator.ApplyAndProposeXml(additionsData, spawn.Additions, this, "additions");
        SuggestedActionApplicator.ExplainForReport(spawn.Additions, report, ExtraReportGroup.ProposedAdditions);
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
        
        // List extrenal data items issues
        
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
        InvalidHamlets,
        ExternalDataMatchingIssues,
        ProposedChanges,
        ProposedAdditions
    }
}
