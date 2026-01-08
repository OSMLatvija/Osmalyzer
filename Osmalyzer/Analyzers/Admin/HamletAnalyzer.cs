using WikidataSharp;

namespace Osmalyzer;

[UsedImplicitly]
public class HamletAnalyzer : Analyzer
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

        VillagesWikidataData villagesWikidataData = datas.OfType<VillagesWikidataData>().First();
        
        ParishesWikidataData parishesWikidataData = datas.OfType<ParishesWikidataData>().First();
        
        VdbAnalysisData vdbData = datas.OfType<VdbAnalysisData>().First();
        
        // Assign VDB data

        vdbData.AssignToDataItems(
            addressData.Hamlets,
            (i, vdb) => 
                i.Name == vdb.Name &&
                vdb.ObjectType == VdbEntryObjectType.Hamlet &&
                vdb.IsActive &&
                i.ParishName == vdb.Location1 &&
                i.MunicipalityName == vdb.Location2,
            10000,
            out List<VdbMatchIssue> vdbMatchIssues
        );
        
        // Assign WikiData

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
                "These hamlets are not currently matched to OSM and can potentially be added based on the source data items."
            );
            
#if DEBUG
            OsmData additionsData = osmMasterData.Copy();
            List<SuggestedAction> suggestedAdditions = [ ];
#endif
            
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
                new DoesntHaveValue("\nhistoric:railway", "station"),
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
            
#if DEBUG
            SuggestedActionApplicator.ApplyAndProposeXml(additionsData, suggestedAdditions, this, "additions");
            SuggestedActionApplicator.ExplainForReport(suggestedAdditions, report, ExtraReportGroup.SuggestedHamletAdditions);
#endif
        }
        
        // Validate hamlet syntax
        
        Validator<Hamlet> hamletValidator = new Validator<Hamlet>(
            hamletCorrelation,
            "Hamlet syntax issues"
        );

        List<SuggestedAction> suggestedChanges = hamletValidator.Validate(
            report,
            false, false,
            new ValidateElementHasValue("place", "hamlet"),
            new ValidateElementValueMatchesDataItemValue<Hamlet>("ref:LV:addr", h => h.AddressID, [ "ref" ]),
            new ValidateElementValueMatchesDataItemValue<Hamlet>("wikidata", h => h.WikidataItem?.QID),
            new ValidateElementHasValue("designation", "mazciems")
        );

#if DEBUG
        SuggestedActionApplicator.ApplyAndProposeXml(osmMasterData, suggestedChanges, this, "changes");
        SuggestedActionApplicator.ExplainForReport(suggestedChanges, report, ExtraReportGroup.ProposedChanges);
#endif
        
        // List invalid hamlets that are still in data
        
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
        
        // Check that Wikidata values match OSM values
        
        // TODO:
        // TODO:
        // TODO:
        
        // List extra data items from non-OSM that were not matched
        
        report.AddGroup(
            ExtraReportGroup.ExternalDataMatchingIssues,
            "Extra data item matching issues",
            "This section lists any issues with data item matching ti additional external data sources.",
            "No issues found."
        );

        // todo: restore when wikidata is fixed, otherwise we small all the regular villages too 
        // List<WikidataItem> extraWikidataItems = villagesWikidataData.Hamlets
        //                                                             .Where(wd => addressData.Hamlets.All(c => c.WikidataItem != wd))
        //                                                             .ToList();
        //
        // foreach (WikidataItem wikidataItem in extraWikidataItems)
        // {
        //     string? name = wikidataItem.GetBestName("lv") ?? null;
        //
        //     report.AddEntry(
        //         ExtraReportGroup.ExternalDataMatchingIssues,
        //         new IssueReportEntry(
        //             "Wikidata village item " + wikidataItem.WikidataUrl + (name != null ? " `" + name + "` " : "") + " was not matched to any OSM element."
        //         )
        //     );
        // }

        foreach (WikidataData.WikidataMatchIssue matchIssue in wikidataMatchIssues)
        {
            switch (matchIssue)
            {
                case WikidataData.MultipleWikidataMatchesWikidataMatchIssue<Hamlet> multipleWikidataMatches:
                    report.AddEntry(
                        ExtraReportGroup.ExternalDataMatchingIssues,
                        new IssueReportEntry(
                            multipleWikidataMatches.DataItem.ReportString() + " matched multiple Wikidata items: " +
                            string.Join(", ", multipleWikidataMatches.WikidataItems.Select(wd => wd.WikidataUrl))
                        )
                    );
                    break;
                
                case WikidataData.CoordinateMismatchWikidataMatchIssue<Hamlet> coordinateMismatch:
                    report.AddEntry(
                        ExtraReportGroup.ExternalDataMatchingIssues,
                        new IssueReportEntry(
                            coordinateMismatch.DataItem.ReportString() + " matched a Wikidata item, but the Wikidata coordinate is too far at " +
                            coordinateMismatch.DistanceMeters.ToString("F0") + " m" +
                            " -- " + coordinateMismatch.WikidataItem.WikidataUrl
                        )
                    );
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(matchIssue));
            }
        }

        foreach (VdbMatchIssue vdbMatchIssue in vdbMatchIssues)
        {
            switch (vdbMatchIssue)
            {
                case MultipleVdbMatchesVdbMatchIssue<Hamlet> multipleVdbMatches:
                    report.AddEntry(
                        ExtraReportGroup.ExternalDataMatchingIssues,
                        new IssueReportEntry(
                            multipleVdbMatches.DataItem.ReportString() + " matched multiple VDB entries: " +
                            string.Join(", ", multipleVdbMatches.VdbEntries.Select(vdb => vdb.ReportString()))
                        )
                    );
                    break;
                
                case CoordinateMismatchVdbMatchIssue<Hamlet> coordinateMismatch:
                    report.AddEntry(
                        ExtraReportGroup.ExternalDataMatchingIssues,
                        new IssueReportEntry(
                            coordinateMismatch.DataItem.ReportString() + " matched a VDB entry, but the VDB coordinate is too far at " +
                            coordinateMismatch.DistanceMeters.ToString("F0") + " m" +
                            " -- " + coordinateMismatch.VdbEntry.ReportString()
                        )
                    );
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(vdbMatchIssue));
            }
        }
    }


    private enum ExtraReportGroup
    {
        SuggestedHamletAdditions,
        InvalidHamlets,
        ExternalDataMatchingIssues,
        ProposedChanges
    }
}
