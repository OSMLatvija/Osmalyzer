using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;

namespace Osmalyzer;

[UsedImplicitly]
public class VPVKACAnalyzer : Analyzer
{
    public override string Name => "VPVKAC Offices";

    public override string Description => "This report checks that VPVKAC (Valsts un pašvaldību vienotie klientu apkalpošanas centri) offices are mapped.";

    public override AnalyzerGroup Group => AnalyzerGroup.StateServices;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData), typeof(VPVKACAnalysisData) ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmData OsmData = osmData.MasterData;

        OsmData osmOffices = OsmData.Filter(
            new IsNode(),
            new HasValue("office", "government")
        );

        // Get office data

        VPVKACAnalysisData officeData = datas.OfType<VPVKACAnalysisData>().First();

        List<VPVKACOffice> offices = officeData.Offices;
        
        // Get office location from raw addresses

        List<LocatedVPVKACOffice> locatedOffices = [ ];
        
        List<VPVKACOffice> unlocatedOffices = [ ];

        foreach (VPVKACOffice office in offices)
        {
            LocatedVPVKACOffice? locateOffice = TryLocateOffice(office, osmData.MasterData);
            
            if (locateOffice != null)
                locatedOffices.Add(locateOffice);
            else
                unlocatedOffices.Add(office);
        }
            
        // Prepare data comparer/correlator

        Correlator<LocatedVPVKACOffice> correlator = new Correlator<LocatedVPVKACOffice>(
            osmOffices,
            locatedOffices,
            new MatchDistanceParamater(100),
            new MatchFarDistanceParamater(300),
            new MatchCallbackParameter<LocatedVPVKACOffice>(GetMatchStrength),
            new OsmElementPreviewValue("name", false),
            new DataItemLabelsParamater("VPVKAC office", "VPVKAC offices"),
            new LoneElementAllowanceParameter(DoesOsmElementLookLikeVPVKACOffice),
            new MatchLoneElementsOnStrongMatchParamater(MatchStrength.Strong) // allow strong matches regardless of distance because we can fail to parse address, but if the OSM element is added, we still want to match assuming it was added correctly
        );

        [Pure]
        MatchStrength GetMatchStrength(LocatedVPVKACOffice office, OsmElement osmElement)
        {
            string? official_name = osmElement.GetValue("official_name");

            if (official_name == office.Office.Name)
                return MatchStrength.Strong; // exact match on official name
            
            string? name = osmElement.GetValue("name");

            if (name == office.Office.Name)
                return MatchStrength.Strong; // exact match on full name

            if (name == office.Office.DisplayName)
                return MatchStrength.Strong; // exact match on disambiguated display name

            if (DoesOsmElementLookLikeVPVKACOffice(osmElement))
                return MatchStrength.Good; // looks like a VPVKAC office, but not exact match
            
            return MatchStrength.Unmatched;
        }

        [Pure]
        bool DoesOsmElementLookLikeVPVKACOffice(OsmElement element)
        {
            string? name = element.GetValue("name");
            
            if (name != null && DoesNameLookLikeVPVKAC(name))
                return true; // looks like a VPVKAC office
            
            string? officialName = element.GetValue("official_name");
            
            if (officialName != null && DoesNameLookLikeVPVKAC(officialName))
                return true; // looks like a VPVKAC office

            return false;
            
            
            [Pure]
            bool DoesNameLookLikeVPVKAC(string value)
            {
                value = value.ToLower();
                
                return 
                    value.Contains("vpvkac") || 
                    value.Contains("valsts un pašvaldības vienotais klientu apkalpošanas centrs");
            }
        }

        // Parse and report primary matching and location correlation

        CorrelatorReport correlation = correlator.Parse(
            report,
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(true),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch()
        );

        // Report any offices we couldn't find an address for

        if (unlocatedOffices.Count > 0)
        {
            report.AddGroup(
                ExtraReportGroup.UnlocatedOffices,
                "Non-geolocated Offices",
                "These listed offices could not be geolocated to an OSM address. " +
                "Possibly, the data values are incorrect, differently-formatted or otherwise fail to match automatically."
            );
            
            foreach (VPVKACOffice unlocatedOffice in unlocatedOffices)
            {
                FuzzyAddress? parsed = FuzzyAddressParser.TryParseAddress(unlocatedOffice.Address);

                report.AddEntry(
                    ExtraReportGroup.UnlocatedOffices,
                    new IssueReportEntry(
                        "Office `" + unlocatedOffice.Name + "` could not be geolocated for `" + unlocatedOffice.Address + "`" +
                        (parsed != null ? " (parsed into: " + string.Join(", ", parsed.Parts.Select(p => p.GetQuickString())) + ")" : "")
                    )
                );
            }
        }
        
        // Offer syntax for quick OSM addition for unmatched located offices
        
        List<LocatedVPVKACOffice> unmatchedLocatedOffices = correlation.Correlations
            .OfType<UnmatchedItemCorrelation<LocatedVPVKACOffice>>()
            .Select(c => c.DataItem)
            .ToList();

        if (unmatchedLocatedOffices.Count > 0)
        {
            report.AddGroup(
                ExtraReportGroup.SuggestedAdditions,
                "Suggested Additions",
                "These VPVKAC offices are not currently matched to OSM and can be added with these (suggested) tags. " +
                "(Address fields are not offered because the local bot will update these automatically on OSM.)"
            );

#if DEBUG
            OsmData additionsData = OsmData.Copy();
            List<SuggestedAction> suggestedAdditions = [ ];
#endif

            foreach (LocatedVPVKACOffice locatedOffice in unmatchedLocatedOffices)
            {
#if DEBUG
                OsmNode newOfficeNode = additionsData.CreateNewNode(locatedOffice.Coord);
#else
                OsmNode newOfficeNode = OsmData.CreateNewNode(locatedOffice.Coord); // not using for actual data/changes, but need for actions to print out the tags
#endif
                
                List<SuggestedAction> actionsForThisNode = [ ];
                
                actionsForThisNode.Add(new OsmCreateElementAction(newOfficeNode));
                actionsForThisNode.Add(new OsmSetValueSuggestedAction(newOfficeNode, "name", string.IsNullOrWhiteSpace(locatedOffice.Office.DisplayName) ? locatedOffice.Office.Name : locatedOffice.Office.DisplayName));
                actionsForThisNode.Add(new OsmSetValueSuggestedAction(newOfficeNode, "official_name", FullName(locatedOffice.Office.Name)));
                actionsForThisNode.Add(new OsmSetValueSuggestedAction(newOfficeNode, "office", "government"));
                actionsForThisNode.Add(new OsmSetValueSuggestedAction(newOfficeNode, "government", "public_service"));
                if (!string.IsNullOrWhiteSpace(locatedOffice.Office.Email))
                    actionsForThisNode.Add(new OsmSetValueSuggestedAction(newOfficeNode, "email", locatedOffice.Office.Email));
                if (!string.IsNullOrWhiteSpace(locatedOffice.Office.Phone))
                    actionsForThisNode.Add(new OsmSetValueSuggestedAction(newOfficeNode, "phone", locatedOffice.Office.Phone));
                if (!string.IsNullOrWhiteSpace(locatedOffice.Office.OpeningHours))
                    actionsForThisNode.Add(new OsmSetValueSuggestedAction(newOfficeNode, "opening_hours", locatedOffice.Office.OpeningHours));
                
#if DEBUG
                suggestedAdditions.AddRange(actionsForThisNode);
#endif

                report.AddEntry(
                    ExtraReportGroup.SuggestedAdditions,
                    new IssueReportEntry(
                        '`' + locatedOffice.Office.DisplayName + "` office at `" +
                        locatedOffice.Office.Address +
                        "` can be added at " +
                        locatedOffice.Coord.OsmUrl +
                        " as" + Environment.NewLine + SuggestedActionApplicator.GetTagsForSuggestedActionsAsCodeString(actionsForThisNode),
                        locatedOffice.Coord,
                        MapPointStyle.Suggestion
                    )
                );
            }
            
#if DEBUG
            SuggestedActionApplicator.ApplyAndProposeXml(additionsData, suggestedAdditions, this, "additions");
            SuggestedActionApplicator.ExplainForReport(suggestedAdditions, report, ExtraReportGroup.SuggestedAdditions);
#endif
        }
        
        // Validate matched office values
        
        Validator<LocatedVPVKACOffice> validator = new Validator<LocatedVPVKACOffice>(
            correlation,
            "Office tagging issues"
        );

        List<SuggestedAction> suggestedChanges = validator.Validate(
            report,
            false, false,
            new ValidateElementValueMatchesDataItemValue<LocatedVPVKACOffice>(
                "name",
                d => string.IsNullOrWhiteSpace(d.Office.DisplayName) ? d.Office.Name : d.Office.DisplayName
            ),
            new ValidateElementValueMatchesDataItemValue<LocatedVPVKACOffice>(
                "official_name",
                d => string.IsNullOrWhiteSpace(d.Office.Name) ? null : FullName(d.Office.Name)
            ),
            new ValidateElementHasValue("office", "government"),
            new ValidateElementHasValue("government", "public_service"),
            new ValidateElementValueMatchesDataItemValue<LocatedVPVKACOffice>(
                "email",
                d => string.IsNullOrWhiteSpace(d.Office.Email) ? null : d.Office.Email
            ),
            new ValidateElementValueMatchesDataItemValue<LocatedVPVKACOffice>(
                "phone",
                d => string.IsNullOrWhiteSpace(d.Office.Phone) ? null : d.Office.Phone
            ),
            new ValidateElementValueMatchesDataItemValue<LocatedVPVKACOffice>(
                "opening_hours",
                d => string.IsNullOrWhiteSpace(d.Office.OpeningHours) ? null : d.Office.OpeningHours
            ),
            new ValidateElementFixme()
        );

#if DEBUG
        SuggestedActionApplicator.ApplyAndProposeXml(OsmData, suggestedChanges, this);
        SuggestedActionApplicator.ExplainForReport(suggestedChanges, report, ExtraReportGroup.ProposedChanges);
#endif
        
        
        // Validate additional issues
        // todo: like what?

        // List all
        report.AddGroup(
            ExtraReportGroup.AllOffices,
            "All VPVKAC Offices"
        );

        foreach (VPVKACOffice office in officeData.Offices)
        {
            report.AddEntry(
                ExtraReportGroup.AllOffices,
                new IssueReportEntry(
                    office.ReportString(true)
                )
            );
        }
    }


    private static LocatedVPVKACOffice? TryLocateOffice(VPVKACOffice office, OsmData osmData)
    {
        OsmCoord? coord = FuzzyAddressFinder.Find(
            osmData,
            office.Address
        );
        
        if (coord == null)
            return null; // no location found

        return new LocatedVPVKACOffice(
            office,
            coord.Value
        );
    }


    [Pure]
    private static string FullName(string officeName)
    {
        // "Cēsu novada Vecpiebalgas pagasta VPVKAC" -> "Cēsu novada Vecpiebalgas pagasta valsts un pašvaldības vienotais klientu apkalpošanas centrs"
        return officeName.Replace("VPVKAC", "valsts un pašvaldības vienotais klientu apkalpošanas centrs");
    }

    private record LocatedVPVKACOffice(VPVKACOffice Office, OsmCoord Coord) : IDataItem
    {
        public string Name => Office.Name;
        
        public string ReportString() => Office.ReportString(false);
    }
    
    
    private enum ExtraReportGroup
    {
        UnlocatedOffices,
        SuggestedAdditions,
        ProposedChanges,
        AllOffices
    }
}