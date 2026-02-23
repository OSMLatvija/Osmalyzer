namespace Osmalyzer;

[UsedImplicitly]
public class EducationalInstitutionAnalyzer : Analyzer
{
    public override string Name => "Educational Institutions";

    public override string Description => "This report checks and lists educational institutions.";

    public override AnalyzerGroup Group => AnalyzerGroup.POIs;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData) ];


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmData masterData = osmData.MasterData;

        // Filter for educational institutions
        
        OsmData kindergartens = masterData.Filter(new HasValue("amenity", "kindergarten"));
        OsmData schools = masterData.Filter(new HasValue("amenity", "school"));
        OsmData colleges = masterData.Filter(new HasValue("amenity", "college"));
        OsmData universities = masterData.Filter(new HasValue("amenity", "university"));

        // Build local entries list

        List<EducationalInstitutionEntry> entries = [];

        AddEntries(entries, kindergartens, EducationalInstitutionType.Kindergarten);
        AddEntries(entries, schools, EducationalInstitutionType.School);
        AddEntries(entries, colleges, EducationalInstitutionType.College);
        AddEntries(entries, universities, EducationalInstitutionType.University);

        // Prepare report groups

        report.AddGroup(
            ReportGroup.Stats,
            "Statistics",
            "Overview of educational institutions found in OSM data."
        );

        report.AddEntry(
            ReportGroup.Stats,
            new GenericReportEntry(
                $"Total educational institutions: {entries.Count} " +
                $"(kindergartens: {entries.Count(e => e.Type == EducationalInstitutionType.Kindergarten)}, " +
                $"schools: {entries.Count(e => e.Type == EducationalInstitutionType.School)}, " +
                $"colleges: {entries.Count(e => e.Type == EducationalInstitutionType.College)}, " +
                $"universities: {entries.Count(e => e.Type == EducationalInstitutionType.University)})"
            )
        );

        // Add groups for each type

        report.AddGroup(
            ReportGroup.Kindergartens,
            "Kindergartens",
            "Names of kindergartens (preschools) found in OSM, sorted by occurrence count."
        );

        report.AddGroup(
            ReportGroup.Schools,
            "Schools",
            "Names of schools found in OSM, sorted by occurrence count."
        );

        report.AddGroup(
            ReportGroup.Colleges,
            "Colleges",
            "Names of colleges found in OSM, sorted by occurrence count."
        );

        report.AddGroup(
            ReportGroup.Universities,
            "Universities",
            "Names of universities found in OSM, sorted by occurrence count."
        );

        // Report entries grouped by name and type

        ReportEntriesByType(report, entries, EducationalInstitutionType.Kindergarten, ReportGroup.Kindergartens);
        ReportEntriesByType(report, entries, EducationalInstitutionType.School, ReportGroup.Schools);
        ReportEntriesByType(report, entries, EducationalInstitutionType.College, ReportGroup.Colleges);
        ReportEntriesByType(report, entries, EducationalInstitutionType.University, ReportGroup.Universities);


        static void AddEntries(List<EducationalInstitutionEntry> entries, OsmData data, EducationalInstitutionType type)
        {
            foreach (OsmElement element in data.Elements)
                entries.Add(new EducationalInstitutionEntry(element, type));
        }

        static void ReportEntriesByType(
            Report report,
            List<EducationalInstitutionEntry> entries,
            EducationalInstitutionType type,
            ReportGroup group)
        {
            List<EducationalInstitutionEntry> typeEntries = entries
                .Where(e => e.Type == type)
                .ToList();

            if (typeEntries.Count == 0)
            {
                report.AddEntry(
                    group,
                    new GenericReportEntry("No entries found.")
                );
                return;
            }

            // Group by name
            
            Dictionary<string, List<EducationalInstitutionEntry>> byName = new Dictionary<string, List<EducationalInstitutionEntry>>();

            foreach (EducationalInstitutionEntry entry in typeEntries)
            {
                string name = entry.Name ?? "∅";

                if (!byName.ContainsKey(name))
                    byName[name] = [];

                byName[name].Add(entry);
            }

            // Sort by occurrence count descending

            List<KeyValuePair<string, List<EducationalInstitutionEntry>>> sortedGroups = byName
                .OrderByDescending(kvp => kvp.Value.Count)
                .ThenBy(kvp => kvp.Key)
                .ToList();

            foreach (KeyValuePair<string, List<EducationalInstitutionEntry>> nameGroup in sortedGroups)
            {
                string name = nameGroup.Key;
                List<EducationalInstitutionEntry> groupEntries = nameGroup.Value;
                int count = groupEntries.Count;

                // Build report text

                string text = 
                    (name == "∅" ? "unnamed" : $"`{name}`") + 
                    $" × {count}";

                // Add OSM links

                text += " - " + ReportEntryFormattingHelper.ListElements(groupEntries.Select(e => e.Element), 15);

                report.AddEntry(
                    group,
                    new GenericReportEntry(
                        text,
                        new SortEntryDesc(count)
                    )
                );
            }
        }
    }


    private enum ReportGroup
    {
        Stats,
        Kindergartens,
        Schools,
        Colleges,
        Universities
    }


    private enum EducationalInstitutionType
    {
        Kindergarten,
        School,
        College,
        University
    }


    private record EducationalInstitutionEntry(OsmElement Element, EducationalInstitutionType Type)
    {
        public string? Name => Element.GetValue("name");

        public string? OfficialName => Element.GetValue("official_name");
    }

}