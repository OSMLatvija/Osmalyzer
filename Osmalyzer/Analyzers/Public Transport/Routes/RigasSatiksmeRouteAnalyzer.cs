namespace Osmalyzer;

[UsedImplicitly]
public class RigasSatiksmeRouteAnalyzer : PublicTransportRouteAnalyzer<RigasSatiksmeAnalysisData>
{
    public override string Name => "Rigas Satiksme";

        
    protected override string Label => "RS";

    protected override string GtfsDisambiguatorId => "LV-RS";
    protected override bool RegionalService => false;


    protected override void CleanUpGtfsData(GTFSNetwork gtfsNetwork)
    {
        // We don't have any known global issues
    }
}