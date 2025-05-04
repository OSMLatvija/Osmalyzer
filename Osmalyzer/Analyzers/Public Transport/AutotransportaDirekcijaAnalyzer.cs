namespace Osmalyzer;

[UsedImplicitly]
public class AutotransportaDirekcijaAnalyzer : PublicTransportAnalyzer<AutotransportaDirekcijaAnalysisData>
{
    public override string Name => "Autotransporta Direkcija";

        
    protected override string Label => "ATD";
    
    
    protected override void CleanUpGtfsData(GTFSNetwork gtfsNetwork)
    {
        // We don't have any known global issues
    }
}