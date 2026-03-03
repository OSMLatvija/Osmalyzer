namespace Osmalyzer;

[UsedImplicitly]
public class NotaryAnalyzer : Analyzer
{
    public override string Name => "Notary offices";

    public override string Description => "This report checks that all notary offices are found on the map and that they have the correct tags.";

    public override AnalyzerGroup Group => AnalyzerGroup.StateServices;


    public override List<Type> GetRequiredDataTypes() =>
    [
        typeof(LatviaOsmAnalysisData),
        typeof(NotaryAnalysisData)
    ];


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmData osmMasterData = osmData.MasterData;

        OsmData osmNotaryOffices = osmMasterData.Filter(
            new HasValue("office", "notary"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmMasterData), OsmPolygon.RelationInclusionCheck.FuzzyLoose)
        );

        // Load notary data

        NotaryAnalysisData notaryData = datas.OfType<NotaryAnalysisData>().First();

        List<NotaryOfficeData> listedOffices = notaryData.Offices;

        // Prepare data comparer/correlator

        Correlator<NotaryOfficeData> correlator = new Correlator<NotaryOfficeData>(
            osmNotaryOffices,
            listedOffices,
            new MatchDistanceParamater(100),
            new MatchFarDistanceParamater(300),
            new MatchExtraDistanceParamater(MatchStrength.Strong, 500),
            new DataItemLabelsParamater("notary office", "notary offices"),
            new OsmElementPreviewValue("name", true),
            new MatchCallbackParameter<NotaryOfficeData>(GetMatchStrength)
        );

        [Pure]
        MatchStrength GetMatchStrength(NotaryOfficeData office, OsmElement element)
        {
            if (FuzzyAddressMatcher.Matches(element, office.Address))
            {
                if (NameMatches(element, office))
                    return MatchStrength.Strong;

                return MatchStrength.Good;
            }

            if (NameMatches(element, office))
                return MatchStrength.Good;

            return MatchStrength.Regular;
        }

        [Pure]
        static bool NameMatches(OsmElement element, NotaryOfficeData office)
        {
            string? name = element.GetValue("name");
            if (name == null)
                return false;

            // Name could be "Zvērināts notārs Jānis Bērziņš" or just "Jānis Bērziņš" or similar
            return name.Contains(office.Name, StringComparison.InvariantCultureIgnoreCase) ||
                   office.Name.Contains(name, StringComparison.InvariantCultureIgnoreCase);
        }

        // Parse and report primary matching and location correlation

        CorrelatorReport correlatorReport = correlator.Parse(
            report,
            new MatchedPairBatch(),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch(),
            new MatchedLoneOsmBatch(true),
            new UnmatchedOsmBatch()
        );

        // Validate matched office values

        Validator<NotaryOfficeData> validator = new Validator<NotaryOfficeData>(
            correlatorReport,
            "Tagging issues"
        );

        List<SuggestedAction> suggestedChanges = validator.Validate(
            report,
            false, false,
            new ValidateElementHasValue("office", "notary"),
            new ValidateElementValueMatchesDataItemValue<NotaryOfficeData>("name", o => o.Name),
            new ValidateElementHasValue("description", "Zvērināts notārs"),
            new ValidateElementValueMatchesDataItemValue<NotaryOfficeData>("email", o => o.Email),
            new ValidateElementValueMatchesDataItemValue<NotaryOfficeData>("phone", o => o.Phone),
            new ValidateElementValueMatchesDataItemValue<NotaryOfficeData>("opening_hours", o => o.OpeningHours),
            new ValidateElementTagSuffixesMatchDataItemValues<NotaryOfficeData>("language", "yes", o => o.Languages),
            new ValidateElementValueMatchesDataItemValue<NotaryOfficeData>("website", o => o.Website),
            new ValidateElementValueMatchesDataItemValue<NotaryOfficeData>("court", o => o.Court)
        );

#if DEBUG
        SuggestedActionApplicator.ApplyAndProposeXml(osmMasterData, suggestedChanges, this, "changes");
        SuggestedActionApplicator.ExplainForReport(suggestedChanges, report, ExtraReportGroup.ProposedChanges);
#endif

        // List all

        report.AddGroup(
            ExtraReportGroup.AllOffices,
            "All Notary Offices"
        );

        foreach (NotaryOfficeData office in listedOffices)
        {
            report.AddEntry(
                ExtraReportGroup.AllOffices,
                new IssueReportEntry(
                    office.ReportString(true)
                )
            );
        }
    }


    private enum ExtraReportGroup
    {
        AllOffices,
        ProposedChanges
    }
}

