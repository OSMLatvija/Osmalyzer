using F23.StringSimilarity;

namespace Osmalyzer;

[UsedImplicitly]
public class ImproperTranslationAnalyzer : Analyzer
{
    public override string Name => "Improper translations";

    public override string Description => "This analyzer checks for improper translations/transliterations of things like street names. " + Environment.NewLine +
                                          "Note that proposed/expected transliteration is generated automatically and has false positives as it makes mistakes.";

    public override AnalyzerGroup Group => AnalyzerGroup.Validation;


    public override List<Type> GetRequiredDataTypes() =>
    [
        typeof(LatviaOsmAnalysisData),
        typeof(FeatureNameQualifiersAnalysisData)
    ];

    /// <summary> These are the languages we check and know about </summary>
    private readonly List<KnownLanguage> _knownLanguages =
    [
        new KnownLanguage("Russian", "ru"),
        new KnownLanguage("English", "en"),
        new KnownLanguage("Latvian", "lv")
    ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Each language keeps its record of results
        Dictionary<KnownLanguage, LanguageAnalysisResults> results =
            _knownLanguages.ToDictionary(kl => kl, _ => new LanguageAnalysisResults());
        
        Dictionary<string, int> ignoredLanguages = new Dictionary<string, int>();
        List<string> ignoredNames = new List<string>();

        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
        FeatureNameQualifiersAnalysisData nameQualifiersData = datas.OfType<FeatureNameQualifiersAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract osmHighwayElements = osmMasterData.Filter(
            new IsWay(),
            new OrMatch(
                new HasKey("highway"),
                new HasValue("route", "road")
            ),
            new HasKey("name"),
            new HasKeyPrefixed("name:"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.Fuzzy)
        );
        // place=*, boundary = administrative, railway = station

        // Too much of (probably) false positives
        // OsmDataExtract osmPlaceElements = osmMasterData.Filter(
        //     new HasKey("place"),
        //     new DoesntHaveAnyValue("place", "city"),
        //     new HasKey("name"),
        //     new HasKeyPrefixed("name:"),
        //     // Exclude Daugavpils for the time being
        //     //new CustomMatch(_ => !BoundaryHelper.GetDaugavpilsPolygon(osmData.MasterData).ContainsElement(_, OsmPolygon.RelationInclusionCheck.Fuzzy)),
        //     new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.Fuzzy)
        // );
        OsmDataExtract osmAdminBoundariesElements = osmMasterData.Filter(
            new IsWay(),
            new HasValue("boundary", "administrative"),
            new HasKey("name"),
            // filter out cross border objects
            new CustomMatch(e => e.HasKey("name") && !Regex.Match(e.GetValue("name")!, @" [-—/] ").Success),
            new HasKeyPrefixed("name:"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.Fuzzy)
        );
        OsmDataExtract osmRwStationsElements = osmMasterData.Filter(
            new HasValue("railway", "station"),
            new HasKey("name"),
            new HasKeyPrefixed("name:"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.Fuzzy)
        );


        // Parse

        CheckElementsTranliteration(osmHighwayElements.Elements, true, results, nameQualifiersData, ignoredNames, ignoredLanguages);
        // checkElementsTranliteration(osmPlaceElements.Elements, false, results, nameQualifiersData, ignoredNames, ignoredLanguages);
        CheckElementsTranliteration(osmAdminBoundariesElements.Elements, false, results, nameQualifiersData, ignoredNames, ignoredLanguages);
        CheckElementsTranliteration(osmRwStationsElements.Elements, false, results, nameQualifiersData, ignoredNames, ignoredLanguages);
        
        // Report checked languages

        foreach ((KnownLanguage language, LanguageAnalysisResults languageResults) in results)
        {
            report.AddGroup(
                language.Name,
                language.Name + " Issues",
                "This lists any entries that look problematic for some reason. " +
                "Due to large variation of naming, there are certainly false positives. " +
                "Transliteration is approximate and lacks any context (e.g. people's names)."
                //"Currently checking \"Latvian iela\" versus expected transliterated \"улица Russian\"."
            );

            foreach (ProblemFeature problemFeature in languageResults.problemFeatures)
            {
                report.AddEntry(
                    language.Name,
                    new IssueReportEntry(
                        (problemFeature.Elements.Count > 1 ? problemFeature.Elements.Count + " elements have " : "Element has ") +
                        (problemFeature.Issues.Count > 1 ? problemFeature.Issues.Count + " issues" : "an issue") + ": " +
                        string.Join("; ", problemFeature.Issues.Select(i => i.ReportString())) + 
                        " -- " + string.Join(", ", problemFeature.Elements.Take(10).Select(e => e.OsmViewUrl)) +
                        (problemFeature.Elements.Count > 10 ? " and " + (problemFeature.Elements.Count - 10) + " more" : ""),
                        new SortEntryDesc(problemFeature.Elements.Count)
                    )
                );
            }

            if (languageResults.nonExactButGoodEnoughMatches.Count > 0)
            {
                report.AddEntry(
                    language.Name,
                    new GenericReportEntry(
                        "Non-exact but good enough transliterated matches not listed as problems (i.e. an allowance for transliteration errors, but would also allow typos and spelling errors): " +
                        string.Join("; ", languageResults.nonExactButGoodEnoughMatches.Select(m => m.Count + " × `" + m.Actual + "` not `" + m.Expected + "` for `" + m.Source + "`"))
                    )
                );
            }

            if (languageResults.fullMatches.Count > 0)
            {
                report.AddEntry(
                    language.Name,
                    new GenericReportEntry(
                        "There were " + languageResults.fullMatches.Count + " exact expected transliterated matches."
                    )
                );
            }
        }

        // Other unchecked languages

        report.AddGroup(
            GenericReportGroup.OtherLanguages, 
            "Other languages",
            "This lists entries of languages that were not checked. "
        );

        foreach ((string language, int number) in ignoredLanguages)
        {
            report.AddEntry(
                GenericReportGroup.OtherLanguages,
                new IssueReportEntry(
                    "Language '" + language + "' was ignored. " + 
                    number + " " + (number == 1 ? "element has" : "elements have") + " tag `name:" + language + "`",
                    new SortEntryDesc(number)
                )
            );
        }


        report.AddGroup(
            GenericReportGroup.OtherNames, 
            "Ignored names",
            "List of items that were not checked, because their name was not recognized (for example streets that don't have recognized translatable nomenclature)"
        );

        foreach (string n in ignoredNames.Distinct().Where(n => !n.Contains('—')))
        {
            report.AddEntry(
                GenericReportGroup.OtherNames,
                new IssueReportEntry(
                    "Name '" + n + "' was ignored. " 
                    //number + " " + (number == 1 ? "element has" : "elements have") + " tag `name:" + language + "`",
                    //new SortEntryDesc(number)
                )
            );
        }
        
        
    }

    
    private void CheckElementsTranliteration(
        IReadOnlyList<OsmElement> elements, 
        bool nomenclatureRequired,
        Dictionary<KnownLanguage, LanguageAnalysisResults> results,
        FeatureNameQualifiersAnalysisData nameQualifiersData,
        List<string> ignoredNames,
        Dictionary<string, int> ignoredLanguages
    )
    {
        foreach (OsmElement element in elements)
        {
            string name = element.GetValue("name")!;

            // Main name
            bool isSuffixFound = ExtractNomenclature(name, nameQualifiersData.Names.Keys.ToList(), out string lvNameRaw, out string? latvianNameSuffix);
            
            if (nomenclatureRequired && !isSuffixFound)
            {
                ignoredNames.Add(name);
                // we don't actually know how this name is constructed in Latvian
                continue; 
            }

            // Other languages

            List<(string, string)> nameXxs = element.GetPrefixedValues("name:")!;

            foreach ((string key, string value) in nameXxs)
            {                
                List<Issue> issues = new List<Issue>();

                string? language = ExtractLanguage(key);

                if (language == null)
                {
                    // TODO: report bad language key
                    continue;
                }

                KnownLanguage? knownLanguage = _knownLanguages.FirstOrDefault(kl => kl.OsmSuffix == language);

                if (knownLanguage != null)
                {
                    // We know about this language, so we can check the name
                    
                    // Collect new results into the language-specific container
                    LanguageAnalysisResults languageResults = results[knownLanguage];

                    switch (language)
                    {
                        case "lv":
                        {
                            // Expect exactly the same values as in name
                            List<string> expectedNames = new List<string> {name};
                            CheckTransliteration(value, expectedNames, name, languageResults, issues, knownLanguage, MatchBetweenExact);
                            break;
                        }
                        case "ru":
                        {
                            // Figure out how the street should look like in Russian transliteration

                            List<string> expectedNames = new List<string>();

                            string translit = Transliterator.TransliterateFromLvToRu(lvNameRaw);

                            if (latvianNameSuffix != null)
                            {
                                List<string> expectedRuPrefixes = nameQualifiersData.Names[latvianNameSuffix][language];

                                foreach (string expectedPrefix in expectedRuPrefixes)
                                {
                                    if (Regex.Match(translit, @"\d\s*$").Success)
                                    {
                                        // For names like 'Imantas 1. līnija' -> 'Имантас 1-я линия'
                                        expectedNames.Add(translit + "-я " + expectedPrefix);
                                        expectedNames.Add(translit + "-й " + expectedPrefix);
                                    }
                                    else 
                                    {
                                        expectedNames.Add(expectedPrefix + " " + translit);
                                        expectedNames.Add(translit + " " + expectedPrefix);
                                    }
                                }
                            }
                            else
                            {
                                expectedNames.Add(translit);
                            }

                            // Match against current value
                            CheckTransliteration(value, expectedNames, name, languageResults, issues, knownLanguage, MatchBetweenFuzzyCyrillic);

                            break;
                        }
                        case "en":
                        {
                            // Handle names like '12th street' and '2nd Line'
                            string translit = Transliterator.TransliterateFromLvToEn(lvNameRaw);
                            
                            List<string> expectedNames = new List<string>();

                            if (latvianNameSuffix != null)
                            {
                                List<string> expectedEnPrefixes = nameQualifiersData.Names[latvianNameSuffix][language];

                                // Expect exact name with only translation for the nomenclature
                                foreach (string expectedPrefix in expectedEnPrefixes)
                                {
                                    expectedNames.Add(translit + " " + expectedPrefix);
                                }
                            }
                            else
                            {
                                expectedNames.Add(translit);
                            }
                            CheckTransliteration(value, expectedNames, name, languageResults, issues, knownLanguage, MatchBetweenExact);

                            break;
                        }
                        
                        default:
                            throw new NotImplementedException();
                    }

                    // Did we find any issues for this element?

                    if (issues.Count > 0)
                    {
                        // Any element(s) with this exact issue already?
                        ProblemFeature? existing = languageResults.problemFeatures.FirstOrDefault(f => f.IssuesMatch(issues));

                        // todo: dont add by distance too close

                        if (existing != null)
                            existing.Elements.Add(element); // issues are the same already
                        else
                            languageResults.problemFeatures.Add(new ProblemFeature(new List<OsmElement>() { element }, issues));
                    }
                }
                else
                {
                    if (ignoredLanguages.ContainsKey(language))
                        ignoredLanguages[language]++;
                    else
                        ignoredLanguages.Add(language, 1);
                }
            }
        }
    }

    private static void CheckTransliteration(
        string value, 
        List<string> expectedValues, 
        string originalName, 
        LanguageAnalysisResults languageResults, 
        List<Issue> issues, 
        KnownLanguage knownLanguage,
        Func<string, string, Match> matcher
    )
    {
        List<Match> matches = expectedValues.Select(ev => matcher(value, ev)).ToList();

        Match bestMatch = matches.OrderByDescending(m => m.Quality).First();

        switch (bestMatch)
        {
            case ExactMatch:
                if (languageResults.fullMatches.ContainsKey(value))
                    languageResults.fullMatches[value]++;
                else
                    languageResults.fullMatches[value] = 1;
                break;

            case GoodEnoughMatch:
                NonExactMatch? existing = languageResults.nonExactButGoodEnoughMatches
                                                            .FirstOrDefault(m =>
                                                                                m.Actual == value &&
                                                                                m.Expected == bestMatch.Expected &&
                                                                                m.Source == originalName);

                if (existing != null)
                    existing.Count++;
                else
                    languageResults.nonExactButGoodEnoughMatches.Add(new NonExactMatch(value, bestMatch.Expected, originalName, 1));
                break;

            case NotAMatch:
                issues.Add(new TranslitMismatchIssue(knownLanguage.Name, knownLanguage.OsmSuffix, bestMatch.Expected, value, "name", originalName));
                break;

            default:
                throw new NotImplementedException();
        }
    }

    [Pure]
    private static bool ExtractNomenclature(string name, List<string> nomenclature, out string rawName, out string? nomenclatureName)
    {
        foreach (string s in nomenclature)
        {
            if (name.EndsWith(" " + s))
            {
                nomenclatureName = s;
                rawName = name[..^(s!.Length)].Trim();
                return true;
            }
        }
        rawName = name;
        nomenclatureName = null;
        return false;
    }

    [Pure]
    private static Match MatchBetweenFuzzyCyrillic(string actual, string expectedOriginal)
    {
        return MatchBetweenFuzzy(actual, expectedOriginal, CyrillicNameMatcher.Instance);
    }

    [Pure]
    private static Match MatchBetweenFuzzy(string actual, string expectedOriginal, NameMatcher matcher)
    {
        actual = actual.ToLower();
        string expectedLower = expectedOriginal.ToLower();

        if (actual == expectedLower)
            return new ExactMatch(expectedOriginal);

        WeightedLevenshtein l = new WeightedLevenshtein(matcher);

        double distance = l.Distance(actual, expectedLower);

        return distance <= 0.5 ? new GoodEnoughMatch(expectedOriginal) : new NotAMatch(expectedOriginal);
    }

    [Pure]
    private static Match MatchBetweenExact(string actual, string original)
    {
        return
            string.Equals(actual, original, StringComparison.CurrentCultureIgnoreCase) ?
                new ExactMatch(original) :
                new NotAMatch(original);
    }

    private abstract record Match(string Expected)
    {
        public abstract int Quality { get; }
    }

    private record ExactMatch(string Expected) : Match(Expected)
    {
        public override int Quality => 100;
    }

    private record GoodEnoughMatch(string Expected) : Match(Expected)
    {
        public override int Quality => 69;
    }

    private record NotAMatch(string Expected) : Match(Expected)
    {
        public override int Quality => -420;
    }

    /// <summary>
    /// Get the language from the key, if it's a language key, such as "en" or "lv" for `name:en` or `name:lv`.
    /// Null means it's not a language, but a key for something else, like `name:etymology` or some such.
    /// </summary>
    [Pure]
    public static string? ExtractLanguage(string key)
    {
        if (key == "name:left" ||
            key == "name:right" ||
            key == "name:wikipedia" ||
            key == "name:pronunciation" ||
            key == "name:prefix" ||
            key == "name:suffix" ||
            key == "name:postfix" ||
            key == "name:full" ||
            key == "name:etymology" ||
            key == "name:carnaval" ||
            key == "name:language" ||
            key == "name:source" ||
            key.Count(c => c == ':') > 1 || // sub-sub keys can do a lot of stuff like specify sources, date ranges, etc.
            Regex.Match(key, @"^name:\d+-(\d+)?$").Success) // date-ranged main name value like `name:2020-` or `name:2020-2021`
        {
            return null;
        }
        
        // Expecting "name:xx" format, which ISO 639-1 code

        if (key.Length < 6)
            return null;

        return key[5..];
    }

    private class ProblemFeature
    {
        public List<OsmElement> Elements { get; }

        public List<Issue> Issues { get; }

        
        public ProblemFeature(List<OsmElement> elements, List<Issue> issues)
        {
            Elements = elements;
            Issues = issues;
        }

        public bool IssuesMatch(List<Issue> issues)
        {
            if (Issues.Count != issues.Count)
                return false;

            foreach (Issue issue in issues)
                if (Issues.All(i => !i.Matches(issue)))
                    return false;

            return true;
        }
    }

    
    private abstract class Issue
    {
        public abstract bool Matches(Issue issue);
        
        public abstract string ReportString();
    }

    private class TranslitMismatchIssue : Issue
    {
        public string Language { get; }
        
        public string MismatchKey { get;}

        public string ExpectedValue { get; }

        public string ActualValue { get; }
        
        public string SourceKey { get; }
        
        public string SourceValue { get; }


        public TranslitMismatchIssue(string language, string mismatchKey, string expectedValue, string actualValue, string sourceKey, string sourceValue)
        {
            Language = language;
            MismatchKey = mismatchKey;
            ExpectedValue = expectedValue;
            ActualValue = actualValue;
            SourceKey = sourceKey;
            SourceValue = sourceValue;
        }


        public override bool Matches(Issue issue)
        {
            if (issue is not TranslitMismatchIssue other)
                return false;

            return
                Language == other.Language &&
                MismatchKey == other.MismatchKey &&
                ExpectedValue == other.ExpectedValue &&
                ActualValue == other.ActualValue &&
                SourceKey == other.SourceKey &&
                SourceValue == other.SourceValue;
        }

        public override string ReportString()
        {
            return "Expected " + Language + " `" + MismatchKey + "` to resemble `" + ExpectedValue + "`, but was `" + ActualValue + "` for `" + SourceKey + "=" + SourceValue + "`";
        }
    }


    private abstract class NameMatcher : ICharacterSubstitution
    {
        public abstract double Cost(char c1, char c2);
    }
        
    private class CyrillicNameMatcher : NameMatcher
    {
        public static NameMatcher Instance { get; } = new CyrillicNameMatcher();

        
        public override double Cost(char c1, char c2)
        {
            c1 = char.ToLower(c1);
            c2 = char.ToLower(c2);
            
            return Math.Min( // we don't care about order, but the comparer seems to care, so we just check both "directions"
                D(c1, c2), 
                D(c2, c1)
            );
            
            static double D(char c1, char c2)
            {
                // Letters that we might fail to transliterate are okay to consider very similar
                
                if (c1 == 'е' && c2 == 'э') return 0.5;
                if (c1 == 'е' && c2 == 'ё') return 0.5;
                if (c1 == 'и' && c2 == 'й') return 0.5;
                if (c1 == 'ш' && c2 == 'щ') return 0.5;
                if (c1 == 'х' && c2 == 'г') return 0.5;
                if (c1 == 'а' && c2 == 'я') return 0.5;
                if (c1 == 'и' && c2 == 'ы') return 0.5;

                return 1.0;
            }
        }
    }
    
    
    private class NonExactMatch
    {
        public string Actual { get; }

        public string Expected { get; }

        public string Source { get; }

        public int Count { get; set; }

        
        public NonExactMatch(string actual, string expected, string source, int count)
        {
            Actual = actual;
            Expected = expected;
            Source = source;
            Count = count;
        }
    }


    private record KnownLanguage(string Name, string OsmSuffix);


    private class LanguageAnalysisResults
    {
        public List<ProblemFeature> problemFeatures { get; } = new List<ProblemFeature>();

        public List<NonExactMatch> nonExactButGoodEnoughMatches { get; } = new List<NonExactMatch>();

        public Dictionary<string, int> fullMatches { get; } = new Dictionary<string, int>();
    }


    private enum GenericReportGroup
    {
        OtherLanguages,
        OtherNames
        // Individual langauges will go to their own group
    }
}