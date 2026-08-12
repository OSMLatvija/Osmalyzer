namespace Osmalyzer;

[UsedImplicitly]
public class JurmalasSatiksmeRouteAnalyzer : PublicTransportRouteAnalyzer<JurmalasSatiksmeAnalysisData>
{
    public override string Name => "Jurmalas Autobusu Satiksme";

        
    protected override string Label => "JS";

    protected override string? GtfsDisambiguatorId => null;
    protected override bool RegionalService => false;
    
    
    protected override void CleanUpGtfsData(GTFSNetwork gtfsNetwork)
    {
        // We don't have any known global issues
    }
}