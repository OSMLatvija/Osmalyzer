using System;
using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer;

[UsedImplicitly]
public class SpellingAnalyzer : Analyzer
{
    public override string Name => "Spelling";

    public override string Description => "This analyzer checks spelling. WIP.";

    public override AnalyzerGroup Group => AnalyzerGroups.Misc;


    public override List<Type> GetRequiredDataTypes() => new List<Type>() 
    {
        typeof(OsmAnalysisData),
        typeof(LatvianHunspellDictionaryAnalysisData)
    };
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();
        
        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract osmElements = osmMasterData.Filter(
            new IsWay(),
            new HasKey("name"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.Fuzzy)
        );
        
        // Spellcheck
        
        LatvianHunspellDictionaryAnalysisData dictionaryData = datas.OfType<LatvianHunspellDictionaryAnalysisData>().First();

        // Parse

        Spellchecker spellchecker = new Spellchecker(dictionaryData);

        // Collect problems grouped by problematic value, so we can list then in bulk
        Dictionary<string, Problem> problems = new Dictionary<string, Problem>();
        
        // Avoid checking things we've seen and decided are ok
        HashSet<string> okValues = new HashSet<string>();

        int ok = 0;
        int misspelled = 0;
        
        foreach (OsmElement element in osmElements.Elements)
        {
            string name = element.GetValue("name")!;

            if (problems.TryGetValue(name, out Problem? problem))
            {
                // Already decided this value is a problem
                
                misspelled++;
                problem.Elements.Add(element);
            }
            else if (!okValues.Contains(name))
            {
                // Not yet seen this value, will now check if it's good/bad
                
                SpellcheckResult result = spellchecker.Check(name);

                switch (result)
                {
                    case OkaySpellcheckResult:
                        ok++;
                        // Don't recheck this in futrure
                        okValues.Add(name);
                        break;

                    case MisspelledSpellcheckResult misspelledResult:
                        misspelled++;
                        problems.Add(name, new Problem(name, element, misspelledResult));
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(result));
                }
            }
            else
            {
                // Seen this value before and it was ok
                
                ok++;
            }
        }
        
        // Report
        
        report.AddGroup(
            ReportGroup.SpellingIssues, 
            "Spelling issues",
            "Note that this uses a spellchecking dictionary, so it doesn't know about proper names and things like brands. This is currently an exercise in false positives."
        );

        report.AddEntry(
            ReportGroup.SpellingIssues,
            new GenericReportEntry(
                "There are " + problems.Count + " unknown-spelling values from " + misspelled + " (out of " + ok + ") elements"
            )
        );

        foreach (Problem problem in problems.Values)
        {
            report.AddEntry(
                ReportGroup.SpellingIssues,
                new IssueReportEntry(
                    (problem.Elements.Count > 1 ? problem.Elements.Count + " elements have " : "Element has ") +
                    " name `" + problem.Value + "` with " +
                    (problem.Result.Misspellings.Count > 1 ? problem.Result.Misspellings.Count + " unknown spellings" : "an unknown spelling") + ": " +
                    string.Join(", ", problem.Result.Misspellings.Select(m => "`" + m.Word + "`")) +
                    " -- " + string.Join(", ", problem.Elements.Take(10).Select(e => e.OsmViewUrl)) +
                    (problem.Elements.Count > 10 ? " and " + (problem.Elements.Count - 10) + " more" : ""),
                    new SortEntryDesc(problem.Elements.Count)
                )
            );
        }
    }
    
    
    private class Problem
    {
        public string Value { get; }
        
        public List<OsmElement> Elements { get; }

        public MisspelledSpellcheckResult Result { get; }

        
        public Problem(string value, OsmElement element, MisspelledSpellcheckResult result)
        {
            Value = value;
            Elements = new List<OsmElement>() { element };
            Result = result;
        }
    }
    
    
    private enum ReportGroup
    {
        SpellingIssues
    }
}