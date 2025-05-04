namespace Osmalyzer;

[UsedImplicitly]
public class RigasSatiksmeAnalyzer : PublicTransportAnalyzer<RigasSatiksmeAnalysisData>
{
    public override string Name => "Rigas Satiksme";

        
    protected override string Label => "RS";
    
    
    protected override void CleanUpGtfsData(GTFSNetwork gtfsNetwork)
    {
        // We don't have any known global issues
    }
}