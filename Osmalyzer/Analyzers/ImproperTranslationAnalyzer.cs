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


    public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData) };
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract osmElements = osmMasterData.Filter(
            new IsWay(),
            new HasKey("highway"),
            new HasKey("name"),
            new HasKeyPrefixed("name:"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.Fuzzy)
        );

        // Parse

        List<ProblemFeature> problemFeatures = new List<ProblemFeature>();

        List<NonExactMatch> nonExactButGoodEnoughMatches = new List<NonExactMatch>();

        Dictionary<string, int> fullMatches = new Dictionary<string, int>();

        foreach (OsmElement element in osmElements.Elements)
        {
            List<Issue> issues = new List<Issue>();
            
            
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

                string? language = ExtractLanguage(key);

                if (language == null)
                {
                    // TODO: report bad language key
                    continue;
                }

                if (language == "lv")
                    continue; // we assume we are by default in Latvian
                // todo: check if mismatch?
                
                switch (language)
                {
                    case "ru":
                    {
                        // Figure out how the street should look like in Russian transliteration
                        
                        string expectedRuPrefix = LatvianToRussianStreetNameSuffix(latvianNameSuffix!);

                        List<string> expectedNames = new List<string>();
                        
                        expectedNames.Add(expectedRuPrefix + " " + Transliterator.TransliterateFromLvToRu(lvNameRaw));
                        expectedNames.Add(Transliterator.TransliterateFromLvToRu(lvNameRaw) + " " + expectedRuPrefix);

                        if (expectedRuPrefix == "улица")
                        {
                            // Also try short prefix
                            expectedNames.Add("ул. " + Transliterator.TransliterateFromLvToRu(lvNameRaw));
                        }
                        else
                        {
                            // Also try default prefix value
                            expectedNames.Add("улица " + Transliterator.TransliterateFromLvToRu(lvNameRaw));
                            expectedNames.Add(Transliterator.TransliterateFromLvToRu(lvNameRaw) + " улица");
                            expectedNames.Add("ул. " + Transliterator.TransliterateFromLvToRu(lvNameRaw));
                        }

                        // Match against current value
                        
                        List<Match> matches = expectedNames.Select(en => MatchBetween(value, en, CyrillicNameMatcher.Instance)).ToList(); 
                        
                        Match bestMatch = matches.OrderByDescending(m => m.Quality).First();

                        switch (bestMatch)
                        {
                            case ExactMatch:
                                if (fullMatches.ContainsKey(value))
                                    fullMatches[value]++;
                                else
                                    fullMatches[value] = 1;
                                break;
                            
                            case GoodEnoughMatch:
                                NonExactMatch? existing = nonExactButGoodEnoughMatches
                                    .FirstOrDefault(m => 
                                                        m.Actual == value && 
                                                        m.Expected == bestMatch.Expected && 
                                                        m.Source == name);
                                
                                if (existing != null)
                                    existing.Count++;
                                else
                                    nonExactButGoodEnoughMatches.Add(new NonExactMatch(value, bestMatch.Expected, name, 1));
                                break;
                            
                            case NotAMatch:
                                issues.Add(new TranslitMismatchIssue("Russian", key, bestMatch.Expected, value, "name", name));
                                break;
                            
                            default:
                                throw new NotImplementedException();
                        }
                        
                        break;
                    }
                }
                
                // todo: list languages we ignore 
            }
            
            
            // Did we find any issues for this element?
            
            if (issues.Count > 0)
            {
                // Any element(s) with this exact issue already?
                ProblemFeature? existing = problemFeatures.FirstOrDefault(f => f.IssuesMatch(issues));

                // todo: dont add by distance too close
                
                if (existing != null)
                    existing.Elements.Add(element); // issues are the same already
                else
                    problemFeatures.Add(new ProblemFeature(new List<OsmElement>() { element }, issues));
            }
        }
        
        // todo: list languages we don't know hwo to compare
        
        // Report
        
        report.AddGroup(
            ReportGroup.Issues, 
            "Issues",
            "This lists any entries that look problematic for some reason. " +
            "Due to large variation of naming, there are certainly false positives. " +
            "Transliteration is approximate and lacks any context (e.g. people's names)." + Environment.NewLine +
            "Currently checking \"Latvian iela\" versus expected transliterated \"улица Russian\"."
        );

        foreach (ProblemFeature problemFeature in problemFeatures)
        {
            report.AddEntry(
                ReportGroup.Issues,
                new IssueReportEntry(
                    (problemFeature.Elements.Count > 1 ? problemFeature.Elements.Count + " elements have " : "Element has ") +
                    (problemFeature.Issues.Count > 1 ? problemFeature.Issues.Count + " issues" : "issue") + ": " +
                    string.Join("; ", problemFeature.Issues.Select(i => i.ReportString())) + " -- " +
                    string.Join(", ", problemFeature.Elements.Take(10).Select(e => e.OsmViewUrl)) +
                    (problemFeature.Elements.Count > 10 ? " and " + (problemFeature.Elements.Count - 10) + " more" : "")
                )
            );
        }

        if (nonExactButGoodEnoughMatches.Count > 0)
        {
            report.AddEntry(
                ReportGroup.Issues,
                new GenericReportEntry(
                    "Non-exact but good enough transliterated matches not listed as problems (i.e. an allowance for transliteration errors, but would also allow typos and spelling errors): " +
                    string.Join("; ", nonExactButGoodEnoughMatches.Select(m => m.Count + " × `" + m.Actual + "` not `" + m.Expected + "` for `" + m.Source + "`"))
                )
            );
        }
        
        if (fullMatches.Count > 0)
        {
            report.AddEntry(
                ReportGroup.Issues,
                new GenericReportEntry(
                    "There were " + fullMatches.Count + " exact expected transliterated matches."
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
    private static Match MatchBetween(string actual, string expectedOriginal, NameMatcher matcher)
    {
        actual = actual.ToLower();
        string expectedLower = expectedOriginal.ToLower();

        if (actual == expectedLower)
            return new ExactMatch(expectedOriginal);

        WeightedLevenshtein l = new WeightedLevenshtein(matcher);

        double distance = l.Distance(actual, expectedLower);

        return distance <= 2.0 ? new GoodEnoughMatch(expectedOriginal) : new NotAMatch(expectedOriginal);
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
            key.Count(c => c == ':') > 1) // sub-sub keys can do a lot of stuff like specify sources, date ranges, etc.
        {
            return null;
        }
        
        // Expecting "name:xx" format, which ISO 639-1 code

        if (key.Length < 6)
            return null;

        return key[5..];
    }

    [Pure]
    private static string LatvianToRussianStreetNameSuffix(string suffix)
    {
        return suffix switch
        {
            "iela"      => "улица",
            "bulvāris"  => "бульвар",
            "ceļš"      => "дорога",
            "gatve"     => "гатве", // apparently, not translated but transliterated
            "šoseja"    => "шоссе",
            "tilts"     => "мост",
            "dambis"    => "дамбис",
            "aleja"     => "аллея",
            "apvedceļš" => "окружная дорога",
            "laukums"   => "площадь",
            "prospekts" => "проспект",
            "pārvads"   => "переезд",
            
            _ => "улица"
        };
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


    private enum ReportGroup
    {
        Issues
    }
}