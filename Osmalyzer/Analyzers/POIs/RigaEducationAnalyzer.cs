namespace Osmalyzer;

[UsedImplicitly]
public class RigaEducationAnalyzer : Analyzer
{
    public override string Name => "Riga Education Institutions";

    public override string Description => "This report checks that Riga education institutions (schools and preschools) listed in the IKSD open data are mapped.";

    public override AnalyzerGroup Group => AnalyzerGroup.POIs;


    public override List<Type> GetRequiredDataTypes() => [typeof(LatviaOsmAnalysisData), typeof(RigaEducationAnalysisData)];


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmData osmMasterData = osmData.MasterData;

        OsmPolygon rigaPolygon = BoundaryHelper.GetRigaPolygon(osmMasterData);

        OsmData osmSchools = osmMasterData.Filter(
            new HasValue("amenity", "school"),
            new InsidePolygon(rigaPolygon, OsmPolygon.RelationInclusionCheck.FuzzyLoose)
        );

        OsmData osmKindergartens = osmMasterData.Filter(
            new HasValue("amenity", "kindergarten"),
            new InsidePolygon(rigaPolygon, OsmPolygon.RelationInclusionCheck.FuzzyLoose)
        );

        // Load institution data

        RigaEducationAnalysisData educationData = datas.OfType<RigaEducationAnalysisData>().First();

        List<RigaEducationData> allInstitutions = educationData.Institutions;

        List<RigaEducationData> schools = allInstitutions.Where(i => i.Type == RigaEducationType.School).ToList();
        List<RigaEducationData> preschools = allInstitutions.Where(i => i.Type == RigaEducationType.Preschool).ToList();

        // Resolve locations for institutions that don't have coordinates

        List<RigaEducationData> locatedSchools = [];
        List<RigaEducationData> unlocatedSchools = [];
        ResolveLocations(schools, osmMasterData, locatedSchools, unlocatedSchools);

        List<RigaEducationData> locatedPreschools = [];
        List<RigaEducationData> unlocatedPreschools = [];
        ResolveLocations(preschools, osmMasterData, locatedPreschools, unlocatedPreschools);

        // Report unlocatable institutions

        if (unlocatedSchools.Count > 0 || unlocatedPreschools.Count > 0)
        {
            report.AddGroup(ReportGroup.Unlocated, "Unlocated institutions", "Institutions from the data that could not be placed on the map based on their address or coordinates.");

            foreach (RigaEducationData school in unlocatedSchools)
                report.AddEntry(ReportGroup.Unlocated, new IssueReportEntry("Could not locate " + school.ReportString()));

            foreach (RigaEducationData preschool in unlocatedPreschools)
                report.AddEntry(ReportGroup.Unlocated, new IssueReportEntry("Could not locate " + preschool.ReportString()));
        }

        // Correlate schools

        CorrelateType(osmSchools, locatedSchools, RigaEducationType.School, report);

        // Correlate preschools

        CorrelateType(osmKindergartens, locatedPreschools, RigaEducationType.Preschool, report);
    }


    private static void CorrelateType(OsmData osmElements, List<RigaEducationData> institutions, RigaEducationType type, Report report)
    {
        string labelSingular = RigaEducationData.TypeLabel(type);
        string labelPlural = RigaEducationData.TypeLabelPlural(type);

        Correlator<RigaEducationData> correlator = new Correlator<RigaEducationData>(
            osmElements,
            institutions,
            new MatchDistanceParamater(150), // address-resolved locations can be off
            new MatchFarDistanceParamater(500),
            new MatchExtraDistanceParamater(MatchStrength.Strong, 500),
            new DataItemLabelsParamater(labelSingular, labelPlural),
            new OsmElementPreviewValue("name", false),
            new MatchCallbackParameter<RigaEducationData>(GetMatchStrength)
        );

        [Pure]
        MatchStrength GetMatchStrength(RigaEducationData institution, OsmElement element)
        {
            string? name = element.GetValue("name");

            if (name != null && NamesMatch(institution.Name, name))
                return MatchStrength.Strong;

            string? officialName = element.GetValue("official_name");

            if (officialName != null && NamesMatch(institution.Name, officialName))
                return MatchStrength.Strong;

            // Try address matching as secondary signal
            if (!string.IsNullOrEmpty(institution.Address) && FuzzyAddressMatcher.Matches(element, institution.Address))
                return MatchStrength.Good;

            return MatchStrength.Unmatched;
        }

        correlator.Parse(
            report,
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(false),
            new UnmatchedItemBatch(),
            new UnmatchedOsmBatch(),
            new MatchedFarPairBatch()
        );
    }


    private static void ResolveLocations(List<RigaEducationData> institutions, OsmData osmData, List<RigaEducationData> located, List<RigaEducationData> unlocated)
    {
        foreach (RigaEducationData institution in institutions)
        {
            // If the institution already has valid coordinates, use it directly
            if (institution.Coord.lat != 0 && institution.Coord.lon != 0)
            {
                located.Add(institution);
                continue;
            }

            // Otherwise, try to resolve the address
            if (string.IsNullOrEmpty(institution.Address))
            {
                unlocated.Add(institution);
                continue;
            }

            OsmCoord? coord = FuzzyAddressFinder.Find(
                osmData,
                institution.Address
            );

            if (coord == null)
            {
                unlocated.Add(institution);
                continue;
            }

            // Create a new item with the resolved coordinates
            located.Add(new RigaEducationData(institution.Name, institution.Address, institution.Type, coord.Value));
        }
    }

    [Pure]
    private static bool NamesMatch(string dataName, string osmName)
    {
        // Direct match
        if (string.Equals(dataName, osmName, StringComparison.OrdinalIgnoreCase))
            return true;

        // Trim common prefixes/suffixes that may differ
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

        // Remove common institution type prefixes/suffixes
        name = Regex.Replace(name, @"^Rīgas\s+", "", RegexOptions.IgnoreCase);
        name = Regex.Replace(name, @"\s+pirmsskolas izglītības iestāde$", "", RegexOptions.IgnoreCase);
        name = Regex.Replace(name, @"\s+vidusskola$", "", RegexOptions.IgnoreCase);
        name = Regex.Replace(name, @"\s+pamatskola$", "", RegexOptions.IgnoreCase);
        name = Regex.Replace(name, @"\s+sākumskola$", "", RegexOptions.IgnoreCase);

        return name;
    }


    private enum ReportGroup
    {
        Unlocated
    }
}




