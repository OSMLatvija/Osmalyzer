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
        
        report.AddGroup(
            ReportGroup.SpellingIssues, 
            "Spelling issues",
            "This is currently an exercise in false positives."
        );

        foreach (OsmElement element in osmElements.Elements)
        {
            string name = element.GetValue("name")!;

            Spellchecker spellchecker = new Spellchecker(dictionaryData);

            SpellcheckResult result = spellchecker.Check(name);

            switch (result)
            {
                case OkaySpellcheckResult:
                    // todo: count
                    break;
                
                case MisspelledSpellcheckResult misspelled:
                    report.AddEntry(
                        ReportGroup.SpellingIssues,
                        new IssueReportEntry(
                            "Name `" + name + "` has mispelling" + (misspelled.Misspellings.Count > 1 ? "s" : "") + ": " +
                            string.Join(", ", misspelled.Misspellings.Select(m => "`" + m.Word + "`")) +
                            " -- " + element.OsmViewUrl
                        )
                    );
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(result));
            }
            

        }
    }   
    
    
    private enum ReportGroup
    {
        SpellingIssues
    }
}