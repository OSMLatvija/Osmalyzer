namespace Osmalyzer;

[UsedImplicitly]
public class CspAnalyzer : Analyzer
{
    // todo: similar names with similar coords, like Rejeņi
    
    public override string Name => "CSP Statistics";

    public override string Description => "This report shows overview for the CSP (Centrālā statistikas pārvalde) statistics data.";

    public override AnalyzerGroup Group => AnalyzerGroup.Miscellaneous;


    public override List<Type> GetRequiredDataTypes() => [ typeof(CspPopulationAnalysisData) ];


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        CspPopulationAnalysisData cspPopulationData = datas.OfType<CspPopulationAnalysisData>().First();
        
        // Report overall statistics
        
        report.AddGroup(
            ReportGroup.Population,
            "Population",
            "These are the parsed CSP entries."
        );
        
        report.AddEntry(
            ReportGroup.Population,
            new GenericReportEntry(
                $"Total entries: {cspPopulationData.Entries.Count}"
            )
        );

        foreach (CspAreaType areaType in Enum.GetValuesAsUnderlyingType<CspAreaType>())
        {
            int count = cspPopulationData.Entries.Count(e => e.Type == areaType);
            
            report.AddEntry(
                ReportGroup.Population,
                new GenericReportEntry(
                    $"{areaType} entries: {count}"
                )
            );
        }

        foreach (CspPopulationEntry entry in cspPopulationData.Entries.OrderBy(e => e.Type).ThenBy(e => e.Name))
        {
            report.AddEntry(
                ReportGroup.Population,
                new GenericReportEntry(
                    $"CSP Area {entry.ReportString()}"
                )
            );
        }
    }


    private enum ReportGroup
    {
        Population
    }
}