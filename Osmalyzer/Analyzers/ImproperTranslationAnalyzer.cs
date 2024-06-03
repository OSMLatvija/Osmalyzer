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

            string? lvNameRaw = null;
            
            if (name.EndsWith(" iela"))
                // todo: use FuzzyAddressMatcher suffixes?
            {
                lvNameRaw = name[..^5];
            }
            
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
                        string expectedRuName = 
                            (value.Contains("ул.") ? "ул." : "улица") + " " + // preserve prefix if shortened
                            Transliterator.TransliterateFromLvToRu(lvNameRaw);

                        if (!GoodEnoughNameMatch(value, expectedRuName, CyrillicNameMatcher.Instance, out bool exact))
                        {
                            issues.Add(new TranslitMismatchIssue("Russian", key, expectedRuName, value, "name", name));
                        }
                        else
                        {
                            if (!exact)
                            {
                                NonExactMatch? existing = nonExactButGoodEnoughMatches
                                    .FirstOrDefault(m => 
                                                        m.Actual == value && 
                                                        m.Expected == expectedRuName && 
                                                        m.Source == name);
                                
                                if (existing != null)
                                    existing.Count++;
                                else
                                    nonExactButGoodEnoughMatches.Add(new NonExactMatch(value, expectedRuName, name, 1));
                            }
                            else
                            {
                                if (fullMatches.ContainsKey(value))
                                    fullMatches[value]++;
                                else
                                    fullMatches[value] = 1;
                            }
                        }

                        break;
                    }
                }
                
                // todo: list languages we ignore 
            }
            
            
            // Did we find any issues?
            
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
    private static bool GoodEnoughNameMatch(string actual, string expected, NameMatcher matcher, out bool exact)
    {
        actual = actual.ToLower();
        expected = expected.ToLower();

        if (actual == expected)
        {
            exact = true;
            return true;
        }

        exact = false;
        
        WeightedLevenshtein l = new WeightedLevenshtein(matcher);

        double distance = l.Distance(actual, expected);

        return distance <= 2.0;
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
            return "Expected " + Language + " `" + MismatchKey + "` to be `" + ExpectedValue + "`, but was `" + ActualValue + "` for `" + SourceKey + "=" + SourceValue + "`";
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