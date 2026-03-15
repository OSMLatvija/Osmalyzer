namespace Osmalyzer;

[UsedImplicitly]
public class CulturalCenterAnalyzer : Analyzer
{
    public override string Name => "Cultural Centers";

    public override string Description => "This report checks that cultural centers (kultūras/tautas nami/centri) listed in the open data " +
                                          "from data.gov.lv are found on the map.";

    public override AnalyzerGroup Group => AnalyzerGroup.POIs;


    public override List<Type> GetRequiredDataTypes() =>
    [
        typeof(LatviaOsmAnalysisData),
        typeof(CulturalCenterAnalysisData)
    ];


    /// <summary>
    /// Name keywords that strongly indicate an OSM element is a cultural center
    /// </summary>
    private readonly string[] _culturalCenterNameKeywords =
    [
        "kultūras nams",
        "kultūras centrs",
        "tautas nams",
        "saieta nams",
        "kultūras pils"
    ];


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmData osmMasterData = osmData.MasterData;

        // Cultural centers in OSM are tagged as amenity=community_centre
        OsmData osmCommunityCentres = osmMasterData.Filter(
            new HasValue("amenity", "community_centre"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmMasterData), OsmPolygon.RelationInclusionCheck.FuzzyLoose)
        );

        // Load cultural center data

        CulturalCenterAnalysisData culturalCenterData = datas.OfType<CulturalCenterAnalysisData>().First();

        List<CulturalCenterData> listedCenters = culturalCenterData.CulturalCenters;

        // Resolve locations for centers that don't have coordinates

        List<CulturalCenterData> locatedCenters = [];
        List<CulturalCenterData> unlocatedCenters = [];

        foreach (CulturalCenterData center in listedCenters)
        {
            if (center.Coord.lat != 0 && center.Coord.lon != 0)
            {
                locatedCenters.Add(center);
                continue;
            }

            // Try to resolve address
            if (string.IsNullOrEmpty(center.Address))
            {
                unlocatedCenters.Add(center);
                continue;
            }

            OsmCoord? coord = FuzzyAddressFinder.Find(osmMasterData, center.Address);

            if (coord != null)
                locatedCenters.Add(new CulturalCenterData(center.Name, center.Address, coord.Value));
            else
                unlocatedCenters.Add(center);
        }

        // Prepare data comparer/correlator

        Correlator<CulturalCenterData> correlator = new Correlator<CulturalCenterData>(
            osmCommunityCentres,
            locatedCenters,
            new MatchDistanceParamater(150),
            new MatchFarDistanceParamater(500),
            new MatchExtraDistanceParamater(MatchStrength.Strong, 700),
            new DataItemLabelsParamater("cultural center", "cultural centers"),
            new OsmElementPreviewValue("name", false),
            new MatchCallbackParameter<CulturalCenterData>(GetMatchStrength),
            new LoneElementAllowanceParameter(LooksLikeCulturalCenter)
        );

        [Pure]
        MatchStrength GetMatchStrength(CulturalCenterData center, OsmElement element)
        {
            string? name = element.GetValue("name");

            if (name != null && NamesMatch(center.Name, name))
                return MatchStrength.Strong;

            string? officialName = element.GetValue("official_name");

            if (officialName != null && NamesMatch(center.Name, officialName))
                return MatchStrength.Strong;

            // Try address matching as secondary signal
            if (!string.IsNullOrEmpty(center.Address) && FuzzyAddressMatcher.Matches(element, center.Address))
                return MatchStrength.Good;

            return MatchStrength.Regular;
        }

        [Pure]
        bool LooksLikeCulturalCenter(OsmElement element)
        {
            string? name = element.GetValue("name");

            if (name == null)
                return false;

            string nameLower = name.ToLower();

            foreach (string keyword in _culturalCenterNameKeywords)
                if (nameLower.Contains(keyword))
                    return true;

            return false;
        }

        // Parse and report primary matching and location correlation

        CorrelatorReport correlation = correlator.Parse(
            report,
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(true),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch()
        );

        // Validate matched center values

        Validator<CulturalCenterData> validator = new Validator<CulturalCenterData>(
            correlation
        );

        Validation validation = validator.Validate(
            report,
            false, false,
            new ValidateElementValueMatchesDataItemValue<CulturalCenterData>("name", c => c.Name)
        );

#if DEBUG
        SuggestedActionApplicator.ApplyAndProposeXml(osmMasterData, validation.Changes, this);
        SuggestedActionApplicator.ExplainForReport(validation.Changes, report, ExtraReportGroup.ProposedChanges);
#endif

        // Report any centers we couldn't geolocate

        if (unlocatedCenters.Count > 0)
        {
            report.AddGroup(
                ExtraReportGroup.UnlocatedCenters,
                "Non-geolocated Cultural Centers",
                "These listed cultural centers could not be geolocated from their address. " +
                "Possibly, the data values are incorrect, differently-formatted or otherwise fail to match automatically."
            );

            foreach (CulturalCenterData unlocated in unlocatedCenters)
            {
                report.AddEntry(
                    ExtraReportGroup.UnlocatedCenters,
                    new IssueReportEntry(
                        "Cultural center `" + unlocated.Name + "` could not be geolocated for `" + unlocated.Address + "`"
                    )
                );
            }
        }

        // List all

        report.AddGroup(
            ExtraReportGroup.AllCenters,
            "All Cultural Centers"
        );

        foreach (CulturalCenterData center in listedCenters)
        {
            report.AddEntry(
                ExtraReportGroup.AllCenters,
                new GenericReportEntry(
                    center.ReportString()
                )
            );
        }
    }


    [Pure]
    private static bool NamesMatch(string dataName, string osmName)
    {
        // Direct match
        if (string.Equals(dataName, osmName, StringComparison.OrdinalIgnoreCase))
            return true;

        // Normalize both and compare
        string normalizedData = NormalizeName(dataName);
        string normalizedOsm = NormalizeName(osmName);

        if (string.Equals(normalizedData, normalizedOsm, StringComparison.OrdinalIgnoreCase))
            return true;

        // Check if one contains the other (for abbreviated vs full names)
        if (normalizedData.Length > 5 && normalizedOsm.Length > 5)
            if (normalizedOsm.Contains(normalizedData, StringComparison.OrdinalIgnoreCase) ||
                normalizedData.Contains(normalizedOsm, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }

    [Pure]
    private static string NormalizeName(string name)
    {
        name = name.Trim();

        // Remove common prefixes/suffixes for matching
        name = Regex.Replace(name, @"\s+kultūras (nams|centrs)$", "", RegexOptions.IgnoreCase);
        name = Regex.Replace(name, @"^.+?\s+novada\s+", "", RegexOptions.IgnoreCase);

        return name;
    }


    private enum ExtraReportGroup
    {
        UnlocatedCenters,
        AllCenters,
        ProposedChanges
    }
}

