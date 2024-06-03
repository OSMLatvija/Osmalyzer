using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

                        if (!string.Equals(expectedRuName, value, StringComparison.InvariantCultureIgnoreCase))
                        {
                            issues.Add(new TranslitMismatchIssue(key, expectedRuName, value, "name", name));
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
        
        // Report
        
        report.AddGroup(ReportGroup.Issues, "Issues");

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
        public string MismatchKey { get;}

        public string ExpectedValue { get; }

        public string ActualValue { get; }
        
        public string SourceKey { get; }
        
        public string SourceValue { get; }


        public TranslitMismatchIssue(string mismatchKey, string expectedValue, string actualValue, string sourceKey, string sourceValue)
        {
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

            return MismatchKey == other.MismatchKey &&
                   ExpectedValue == other.ExpectedValue &&
                   ActualValue == other.ActualValue &&
                     SourceKey == other.SourceKey &&
                   SourceValue == other.SourceValue;
        }

        public override string ReportString()
        {
            return "Expected `" + MismatchKey + "` to be `" + ExpectedValue + "`, but was `" + ActualValue + "` for `" + SourceKey + "`=`" + SourceValue + "`";
        }
    }


    private enum ReportGroup
    {
        Issues
    }
}