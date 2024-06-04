using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using F23.StringSimilarity;

namespace Osmalyzer;

[UsedImplicitly]
public class ImproperTranslationAnalyzer : Analyzer
{
    public override string Name => "Improper translations";

    public override string Description => "This analyzer checks for improper translations/transliterations of things like street names. " + Environment.NewLine +
                                          "Note that proposed/expected transliteration is generated automatically and has false positives as it makes mistakes.";

    public override AnalyzerGroup Group => AnalyzerGroups.Misc;


    public override List<Type> GetRequiredDataTypes() => new List<Type>() 
    {
        typeof(OsmAnalysisData),
        typeof(StreetNameQualifiersAnalysisData)
    };
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();
        StreetNameQualifiersAnalysisData nameQualifiersData = datas.OfType<StreetNameQualifiersAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract osmElements = osmMasterData.Filter(
            new IsWay(),
            new HasKey("highway"),
            new HasKey("name"),
            new HasKeyPrefixed("name:"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.Fuzzy)
        );

        // These are the languages we check and know about
        List<KnownLanguage> knownLanguages = new List<KnownLanguage>()
        {
            new KnownLanguage("Russian", "ru"),
            new KnownLanguage("English", "en")
        };

        // Each language keeps its record of results
        Dictionary<KnownLanguage, LanguageAnalysisResults> results =
            knownLanguages.ToDictionary(kl => kl, _ => new LanguageAnalysisResults());
        
        Dictionary<string, int> ignoredLanguages = new Dictionary<string, int>();

        // Parse

        foreach (OsmElement element in osmElements.Elements)
        {
            string name = element.GetValue("name")!;

            // Main name

            string? lvNameRaw = ExtractRawLatvianName(name, out string? latvianNameSuffix);

            // todo: how many do we not know?
            if (lvNameRaw == null)
                continue; // we don't actually know how this name is constructed in Latvian

            // Other languages

            List<(string, string)> nameXxs = element.GetPrefixedValues("name:")!;

            foreach ((string key, string value) in nameXxs)
            {
                if (value == name) // may not be great, but not an error
                    continue;
                
                List<Issue> issues = new List<Issue>();

                string? language = ExtractLanguage(key);

                if (language == null)
                {
                    // TODO: report bad language key
                    continue;
                }

                if (language == "lv")
                {
                    // todo: check if mismatch?
                    continue; // we assume we are by default in Latvian
                }

                KnownLanguage? knownLanguage = knownLanguages.FirstOrDefault(kl => kl.OsmSuffix == language);

                if (knownLanguage != null)
                {
                    // We know about this language, so we can check the name
                    
                    // Collect new results into the language-specific container
                    LanguageAnalysisResults languageResults = results[knownLanguage];

                    switch (language)
                    {
                        case "ru":
                        {
                            // Figure out how the street should look like in Russian transliteration

                            List<string> expectedRuPrefixes = nameQualifiersData.Names[latvianNameSuffix!][language];
                            // It is acceptable for all object to be named as street (why?)
                            expectedRuPrefixes = expectedRuPrefixes.Union(nameQualifiersData.Names["iela"][language]).ToList();

                            List<string> expectedNames = new List<string>();

                            foreach (string expectedPrefix in expectedRuPrefixes)
                            {
                                string translit = Transliterator.TransliterateFromLvToRu(lvNameRaw);
                                expectedNames.Add(expectedPrefix + " " + translit);
                                expectedNames.Add(translit + " " + expectedPrefix);
                            }

                            // Match against current value

                            List<Match> matches = expectedNames.Select(en => MatchBetweenFuzzy(value, en, CyrillicNameMatcher.Instance)).ToList();

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
                                                                                                 m.Source == name);

                                    if (existing != null)
                                        existing.Count++;
                                    else
                                        languageResults.nonExactButGoodEnoughMatches.Add(new NonExactMatch(value, bestMatch.Expected, name, 1));
                                    break;

                                case NotAMatch:
                                    issues.Add(new TranslitMismatchIssue("Russian", key, bestMatch.Expected, value, "name", name));
                                    break;

                                default:
                                    throw new NotImplementedException();
                            }

                            break;
                        }
                        case "en":
                        {
                            List<string> expectedEnPrefixes = nameQualifiersData.Names[latvianNameSuffix!][language];
                            // It is acceptable for all object to be named as street (why?)
                            expectedEnPrefixes = expectedEnPrefixes.Union(nameQualifiersData.Names["iela"][language]).ToList();

                            List<string> expectedNames = new List<string>();

                            // Expect exact name with only translation for the nomenclature
                            foreach (string expectedPrefix in expectedEnPrefixes)
                            {
                                expectedNames.Add(lvNameRaw + " " + expectedPrefix);
                            }

                            // Match against current value

                            List<Match> matches = expectedNames.Select(en => MatchBetweenExact(value, en)).ToList();

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
                                                                                                 m.Source == name);

                                    if (existing != null)
                                        existing.Count++;
                                    else
                                        languageResults.nonExactButGoodEnoughMatches.Add(new NonExactMatch(value, bestMatch.Expected, name, 1));
                                    break;

                                case NotAMatch:
                                    issues.Add(new TranslitMismatchIssue("English", key, bestMatch.Expected, value, "name", name));
                                    break;

                                default:
                                    throw new NotImplementedException();
                            }

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
                        (problemFeature.Issues.Count > 1 ? problemFeature.Issues.Count + " issues" : "issue") + ": " +
                        string.Join("; ", problemFeature.Issues.Select(i => i.ReportString())) + " -- " +
                        string.Join(", ", problemFeature.Elements.Take(10).Select(e => e.OsmViewUrl)) +
                        (problemFeature.Elements.Count > 10 ? " and " + (problemFeature.Elements.Count - 10) + " more" : "")
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

        foreach ((string language, int number) in ignoredLanguages.OrderByDescending(kv => kv.Value))
        {
            report.AddEntry(
                GenericReportGroup.OtherLanguages,
                new IssueReportEntry(
                    "Language '" + language + "' was ignored. " + 
                    number + " " + (number == 1 ? "element has" : "elements have") + " tag `name:" + language + "`"
                )
            );
        }
    }


    [Pure]
    private static string? ExtractRawLatvianName(string name, out string? suffix)
    {
        if (FuzzyAddressMatcher.EndsWithStreetNameSuffix(name, out suffix))
            return name[..^(suffix!.Length + 1)]; // also grab the implied space 

        return null;
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

        return distance <= 2.0 ? new GoodEnoughMatch(expectedOriginal) : new NotAMatch(expectedOriginal);
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

    [Pure]
    private static string? ExtractLanguage(string key)
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
            key.Count(c => c == ':') > 1) // sub-sub keys can do a lot of stuff like specify sources, date ranges, etc.
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
        OtherLanguages
        // Individual langauges will go to their own group
    }
}