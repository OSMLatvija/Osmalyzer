namespace Osmalyzer;

[UsedImplicitly]
public class JurmalasSatiksmeRouteAnalyzer : PublicTransportRouteAnalyzer<JurmalasSatiksmeAnalysisData>
{
    protected override string Label => "Jurmalas Autobusu Satiksme";

    protected override string? GtfsDisambiguatorId => null;
    protected override bool RegionalService => false;
    
    
    protected override void CleanUpGtfsData(GTFSNetwork gtfsNetwork)
    {
        // We don't have any known global issues
    }
}