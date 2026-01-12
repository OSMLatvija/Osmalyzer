namespace Osmalyzer;

[UsedImplicitly]
public class CspAnalyzer : Analyzer
{
    // todo: similar names with similar coords, like Rejeņi
    
    public override string Name => "CSP Population Statistics";

    public override string Description => "This report shows overview for the CSP (Centrālā statistikas pārvalde) population statistics data.";

    public override AnalyzerGroup Group => AnalyzerGroup.Miscellaneous;


    public override List<Type> GetRequiredDataTypes() => [ typeof(CspPopulationAnalysisData) ];


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        CspPopulationAnalysisData cspPopulationData = datas.OfType<CspPopulationAnalysisData>().First();
        
        // Report overall statistics
        
        report.AddGroup(
            ReportGroup.Stats,
            "Overall statistics",
            "This gives an overview of the parsed CSP data."
        );
        
        report.AddEntry(
            ReportGroup.Stats,
            new GenericReportEntry(
                $"Total entries: {cspPopulationData.Entries.Count}"
            )
        );        
        
        // todo:
    }


    private enum ReportGroup
    {
        Stats
    }
}