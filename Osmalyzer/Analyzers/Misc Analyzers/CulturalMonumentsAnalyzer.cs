namespace Osmalyzer;

[UsedImplicitly]
public class CulturalMonumentsAnalyzer : Analyzer
{
    public override string Name => "Cultural Monuments";

    public override string Description => "This report checks that all the hsitorical national cultural monuments are mapped." + Environment.NewLine +
                                          "The registry is maintained by VKPAI (Valsts kultūras pieminekļu aizsardzības inspekcija) and the coordinates are usually very precise (although the actual geometry strongly depends on what it represents - it can be individual points, area, even approximate site)." + Environment.NewLine +
                                          "Note that all sorts of combinations between OSM and heritage entries are possible, such as many elements being a single heritage site or a single element containing multiple (often-unmapped) heritage objects - determining this usually requires manual review.";

    public override AnalyzerGroup Group => AnalyzerGroups.Misc;

    public override List<Type> GetRequiredDataTypes() =>
    [
        typeof(LatviaOsmAnalysisData),
        typeof(CulturalMonumentsMapAnalysisData),
        typeof(CulturalMonumentsWikidataData)
    ];


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract osmHeritages = osmMasterData.Filter(
            new OrMatch(
                new HasAnyKey( // any of the heritage keys automatically "pass" the element, even if it's a weird type
                    "heritage",
                    "heritage:operator",
                    "ref:LV:vkpai"
                ),
                new HasAnyKey( // common targets, but not something we rarely to never expect
                    "building",
                    "ruins",
                    "historic",
                    "man_made",
                    "landuse"
                )
            )
        );

        // Get monument data

        List<CulturalMonument> monuments = datas.OfType<CulturalMonumentsMapAnalysisData>().First().Monuments;

        // Known/ignored names

#if !REMOTE_EXECUTION

        string ignoredNameFileName = @"data/cultural monument ignored names.tsv";

        if (!File.Exists(ignoredNameFileName))
            ignoredNameFileName = @"../../../../" + ignoredNameFileName; // "exit" Osmalyzer\bin\Debug\net6.0\ folder and grab it from root data\

        string[] ignoredNames = File.ReadAllLines(ignoredNameFileName, Encoding.UTF8);

        string knownNameFileName = @"data/cultural monument known names.tsv";

        if (!File.Exists(knownNameFileName))
            knownNameFileName = @"../../../../" + knownNameFileName; // "exit" Osmalyzer\bin\Debug\net6.0\ folder and grab it from root data\

        string[] knownNames = File.ReadAllLines(knownNameFileName, Encoding.UTF8);

        List<string> ignoredMatch = new List<string>() { "name\tignored matches" };
        List<string> knownMatch = new List<string>() { "name\tknown matches" };
        List<string> conflictMatch = new List<string>() { "name\tknown matches\tignored matches" };
        List<string> unknownMatch = new List<string>() { "name" };

        foreach (CulturalMonument monument in monuments)
        {
            List<string> ignoredMatches = ignoredNames.Where(inm => Regex.IsMatch(monument.Name, inm, RegexOptions.IgnoreCase)).ToList();
            bool ignored = ignoredMatches.Any();

            List<string> knownMatches = knownNames.Where(inm => Regex.IsMatch(monument.Name, inm, RegexOptions.IgnoreCase)).ToList();
            bool known = knownMatches.Any();

            if (ignored && known)
                conflictMatch.Add(monument.Name + "\t" + string.Join("; ", knownMatches.Select(m => "\"" + m + "\"")) + "\t" + string.Join("; ", ignoredMatches.Select(m => "\"" + m + "\"")));
            else if (ignored)
                ignoredMatch.Add(monument.Name + "\t" + string.Join("; ", ignoredMatches.Select(m => "\"" + m + "\"")));
            else if (known)
                knownMatch.Add(monument.Name + "\t" + string.Join("; ", knownMatches.Select(m => "\"" + m + "\"")));
            else
                unknownMatch.Add(monument.Name);
        }

        File.WriteAllLines("cmdump-ignored.txt", ignoredMatch);
        File.WriteAllLines("cmdump-known.txt", knownMatch);
        File.WriteAllLines("cmdump-conflict.txt", conflictMatch);
        File.WriteAllLines("cmdump-unknown.txt", unknownMatch);

#endif

        // Assign Wikidata to monument data

        CulturalMonumentsWikidataData wikidataData = datas.OfType<CulturalMonumentsWikidataData>().First();
        wikidataData.Assign(monuments);

        // Prepare data comparer/correlator

        Correlator<CulturalMonument> correlator = new Correlator<CulturalMonument>(
            osmHeritages,
            monuments,
            new MatchDistanceParamater(30),
            new MatchFarDistanceParamater(300),
            new MatchExtraDistanceParamater(MatchStrength.Strong, 1200), // some POIs are like neghbourhoods and large areas
            new DataItemLabelsParamater("monument", "monuments"),
            new MatchCallbackParameter<CulturalMonument>(DoesOsmNodeMatchMonument),
            new OsmElementPreviewValue("name", false),
            new LoneElementAllowanceParameter(IsOsmElementHeritagePoiByItself)
        );

        [Pure]
        MatchStrength DoesOsmNodeMatchMonument(CulturalMonument monument, OsmElement osmElement)
        {
            // name

            if (FuzzyNameMatcher.Matches(osmElement, "name", monument.Name) ||
                FuzzyNameMatcher.Matches(osmElement, "old_name", monument.Name))
                return MatchStrength.Strong;

            // ref:LV:vkpai

            string? osmRefStr = osmElement.GetValue("ref:LV:vkpai");

            if (osmRefStr != null)
            {
                if (int.TryParse(osmRefStr, out int osmRef))
                    if (osmRef == monument.ReferenceID)
                        return MatchStrength.Strong;

                return MatchStrength.Good;
            }

            // heritage

            string? heritageStr = osmElement.GetValue("heritage");

            if (heritageStr != null)
            {
                if (int.TryParse(osmRefStr, out int osmRef))
                    if (osmRef == 2)
                        return MatchStrength.Good;

                return MatchStrength.Regular;
            }

            // heritage:operator

            string? herOperStr = osmElement.GetValue("heritage:operator");

            if (herOperStr != null)
            {
                herOperStr = herOperStr.ToLower();

                if (herOperStr.Contains("vkpai") ||
                    herOperStr.Contains("valsts kultūras pieminekļu aizsardzības inspekcija"))
                    return MatchStrength.Good;

                return MatchStrength.Regular;
            }

            // Wikidata ID

            if (monument.WikidataItem != null)
            {
                string? wikidataStr = osmElement.GetValue("wikidata");

                if (wikidataStr != null && wikidataStr.Length > 1)
                {
                    if (long.TryParse(wikidataStr, out long wikidataID))
                    {
                        if (wikidataID.ToString() == monument.WikidataItem[wikidataData.PropertyID])
                            return MatchStrength.Strong;
                    }
                }
            }

            return MatchStrength.Unmatched;
        }

        [Pure]
        bool IsOsmElementHeritagePoiByItself(OsmElement osmElement)
        {
            // ref:LV:vkpai

            string? osmRefStr = osmElement.GetValue("ref:LV:vkpai");

            if (osmRefStr != null)
                return true;

            // heritage:operator

            string? herOper = osmElement.GetValue("heritage:operator");

            if (herOper != null)
            {
                herOper = herOper.ToLower();

                if (herOper.Contains("vkpai") ||
                    herOper.Contains("valsts kultūras pieminekļu aizsardzības inspekcija"))
                    return true;
            }

            // Wikidata ID
            // If our wikidata item was loaded as a wikidata item with cultural heritage ID, then we must be one

            string? wikidataStr = osmElement.GetValue("wikidata");

            if (wikidataStr != null && wikidataStr.Length > 1)
            {
                if (long.TryParse(wikidataStr, out long wikidataID))
                {
                    string wikidataIDAsStr = wikidataID.ToString();

                    if (wikidataData.Items.Any(i => i[wikidataData.PropertyID] == wikidataIDAsStr))
                        return true;
                }
            }

            return false;
        }

        // Parse and report primary matching and location correlation

        CorrelatorReport correlatorReport = correlator.Parse(
            report,
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(true),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch()
        );

        // Validate additional issues

        Validator<CulturalMonument> validator = new Validator<CulturalMonument>(
            correlatorReport
        );

        validator.Validate(
            report,
            true, // all elements we checked against are "real", so should follow the rules
            new ValidateElementHasAcceptableValue("ref:LV:vkpai", IsKnownMonumentRefID, "known monument ID")
        );

        [Pure]
        bool IsKnownMonumentRefID(string id)
        {
            return monuments.Any(m => m.ReferenceID != null && m.ReferenceID.ToString() == id);
        }
    }
}