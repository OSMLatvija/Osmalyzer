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

        List<ValidationRule> rules = [ 
            new ValidateElementHasValue("office", "notary"),
            new ValidateElementValueMatchesDataItemValue<NotaryOfficeData>("name", o => o.Name),
            new ValidateElementHasValue("description", "Zvērināts notārs"),
            new ValidateElementValueMatchesDataItemValue<NotaryOfficeData>("email", o => o.Email),
            new ValidateElementValueMatchesDataItemValue<NotaryOfficeData>("phone", o => o.Phone),
            new ValidateElementValueMatchesDataItemValue<NotaryOfficeData>("opening_hours", o => o.OpeningHours),
            new ValidateElementTagSuffixesMatchDataItemValues<NotaryOfficeData>("language", "yes", o => o.Languages),
            new ValidateElementValueMatchesDataItemValue<NotaryOfficeData>("website", o => o.Website),
            new ValidateElementValueMatchesDataItemValue<NotaryOfficeData>("court", o => o.Court)
        ];
        
        Validation validation = validator.Validate(
            report,
            false, false,
            rules
        );

#if DEBUG
        SuggestedActionApplicator.ApplyAndProposeXml(osmMasterData, validation.Changes, this, "changes");
        SuggestedActionApplicator.ExplainForReport(validation.Changes, report, ExtraReportGroup.ProposedChanges);
#endif
        
        // Offer syntax for quick OSM addition for unmatched offices

        List<NotaryOfficeData> unmatchedOffices = correlatorReport.Correlations
            .OfType<UnmatchedItemCorrelation<NotaryOfficeData>>()
            .Select(c => c.DataItem)
            .ToList();

        if (unmatchedOffices.Count > 0)
        {
            report.AddGroup(
                ExtraReportGroup.SuggestedAdditions,
                "Suggested Additions",
                "These notary offices are not currently matched to OSM and can be added with these (suggested) tags."
            );

#if DEBUG
            OsmData additionsData = osmMasterData.Copy();
            List<SuggestedAction> suggestedAdditions = [ ];
#endif

            foreach (NotaryOfficeData office in unmatchedOffices)
            {
#if DEBUG
                OsmNode newOfficeNode = additionsData.CreateNewNode(office.Coord);
#else
                OsmNode newOfficeNode = osmMasterData.CreateNewNode(office.Coord); // not using for actual data/changes, but need for actions to print out the tags
#endif

                List<SuggestedAction> actionsForThisNode = [ ];

                actionsForThisNode.Add(new OsmCreateElementAction(newOfficeNode));
                actionsForThisNode.Add(new OsmSetValueSuggestedAction(newOfficeNode, "office", "notary"));
                actionsForThisNode.Add(new OsmSetValueSuggestedAction(newOfficeNode, "name", office.Name));
                actionsForThisNode.Add(new OsmSetValueSuggestedAction(newOfficeNode, "description", "Zvērināts notārs"));
                if (office.Phone != null)
                    actionsForThisNode.Add(new OsmSetValueSuggestedAction(newOfficeNode, "phone", office.Phone));
                if (office.Email != null)
                    actionsForThisNode.Add(new OsmSetValueSuggestedAction(newOfficeNode, "email", office.Email));
                if (office.OpeningHours != null)
                    actionsForThisNode.Add(new OsmSetValueSuggestedAction(newOfficeNode, "opening_hours", office.OpeningHours));
                foreach (string language in office.Languages)
                    actionsForThisNode.Add(new OsmSetValueSuggestedAction(newOfficeNode, "language:" + language, "yes"));
                actionsForThisNode.Add(new OsmSetValueSuggestedAction(newOfficeNode, "website", office.Website));
                actionsForThisNode.Add(new OsmSetValueSuggestedAction(newOfficeNode, "court", office.Court));

#if DEBUG
                suggestedAdditions.AddRange(actionsForThisNode);
#endif

                report.AddEntry(
                    ExtraReportGroup.SuggestedAdditions,
                    new IssueReportEntry(
                        "Notary `" + office.Name + "` at `" + office.FullAddress + "` can be added at " +
                        office.Coord.OsmUrl +
                        " as" + Environment.NewLine + SuggestedActionApplicator.GetTagsForSuggestedActionsAsCodeString(actionsForThisNode),
                        office.Coord,
                        MapPointStyle.Suggestion
                    )
                );
            }

#if DEBUG
            SuggestedActionApplicator.ApplyAndProposeXml(additionsData, suggestedAdditions, this, "additions");
            SuggestedActionApplicator.ExplainForReport(suggestedAdditions, report, ExtraReportGroup.SuggestedAdditions);
#endif
        }

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
        SuggestedAdditions,
        ProposedChanges
    }
}

