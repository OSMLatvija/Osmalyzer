namespace Osmalyzer;

[UsedImplicitly]
public class JelgavasAutobusuParksAnalyzer : PublicTransportAnalyzer<JelgavasAutobusuParksAnalysisData>
{
    public override string Name => "Jelgavas Autobusu Parks";

        
    protected override string Label => "JAP";
    
    
    protected override void CleanUpGtfsData(GTFSNetwork gtfsNetwork)
    {
        // We don't have any known global issues
    }
}