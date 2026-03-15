namespace Osmalyzer;

[UsedImplicitly]
public class CulturalCenterAnalyzer : Analyzer
{
    public override string Name => "Cultural Centers";

    public override string Description => "This report checks that cultural centers (kultūras/tautas nami/centri) listed in the open data " +
                                          "from data.gov.lv are found on the map with the expected tags.";

    public override AnalyzerGroup Group => AnalyzerGroup.POIs;


    public override List<Type> GetRequiredDataTypes() =>
    [
        typeof(LatviaOsmAnalysisData),
        typeof(CulturalCenterAnalysisData)
    ];


    /// <summary>
    /// Name keyword groups that strongly indicate an OSM element is a cultural center.
    /// Each group contains aliases counted together.
    /// </summary>
    private readonly string[][] _culturalCenterNameKeywords =
    [
        [ "kultūras nams" ],
        [ "kultūras centrs" ],
        [ "tautas nams" ],
        [ "saieta nams", "saietu nams" ]
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

        // Prepare data comparer/correlator

        Correlator<CulturalCenterData> correlator = new Correlator<CulturalCenterData>(
            osmCommunityCentres,
            listedCenters,
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

            foreach (string[] group in _culturalCenterNameKeywords)
                foreach (string keyword in group)
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

        // Stats - keyword capitalization variations and names without any known keyword

        report.AddGroup(ExtraReportGroup.Stats, "Stats");

        foreach (string[] group in _culturalCenterNameKeywords)
        {
            Dictionary<string, int> variations = new Dictionary<string, int>();

            foreach (string keyword in group)
            {
                foreach (CulturalCenterData center in listedCenters)
                {
                    int index = center.Name.ToLower().IndexOf(keyword);

                    if (index < 0)
                        continue;

                    string actual = center.Name.Substring(index, keyword.Length);

                    if (variations.TryGetValue(actual, out int existing))
                        variations[actual] = existing + 1;
                    else
                        variations[actual] = 1;
                }
            }

            string groupLabel = string.Join(" / ", group.Select(k => "`" + k + "`"));

            if (variations.Count > 0)
            {
                int total = variations.Values.Sum();

                report.AddEntry(
                    ExtraReportGroup.Stats,
                    new GenericReportEntry(
                        "Keyword " + groupLabel + " ×" + total + ": " +
                        string.Join(", ", variations.OrderByDescending(kv => kv.Value).Select(kv => "`" + kv.Key + "` ×" + kv.Value))
                    )
                );
            }
            else
            {
                report.AddEntry(
                    ExtraReportGroup.Stats,
                    new GenericReportEntry(
                        "Keyword " + groupLabel + " does not appear in any listed center names"
                    )
                );
            }
        }

        // Centers whose names contain none of the known keywords

        List<CulturalCenterData> unknownCenters = [];

        foreach (CulturalCenterData center in listedCenters)
        {
            bool hasKeyword = false;

            foreach (string[] group in _culturalCenterNameKeywords)
            {
                foreach (string keyword in group)
                {
                    if (center.Name.ToLower().Contains(keyword))
                    {
                        hasKeyword = true;
                        break;
                    }
                }

                if (hasKeyword)
                    break;
            }

            if (!hasKeyword)
                unknownCenters.Add(center);
        }

        if (unknownCenters.Count > 0)
        {
            report.AddEntry(
                ExtraReportGroup.Stats,
                new GenericReportEntry(
                    unknownCenters.Count + " centers have no common/known frequent keyword in their name: " +
                    string.Join(", ", unknownCenters.Select(c => "`" + c.Name + "`"))
                )
            );
        }
        else
        {
            report.AddEntry(
                ExtraReportGroup.Stats,
                new GenericReportEntry(
                    "All listed centers have a known keyword in their name"
                )
            );
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
        AllCenters,
        ProposedChanges,
        Stats
    }
}

