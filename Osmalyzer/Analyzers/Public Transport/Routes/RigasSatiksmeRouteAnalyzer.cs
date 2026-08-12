namespace Osmalyzer;

[UsedImplicitly]
public class RigasSatiksmeRouteAnalyzer : PublicTransportRouteAnalyzer<RigasSatiksmeAnalysisData>
{
    protected override string Label => "Rigas Satiksme";

    protected override string GtfsDisambiguatorId => "LV-RS";
    protected override bool RegionalService => false;


    protected override void CleanUpGtfsData(GTFSNetwork gtfsNetwork)
    {
        // We don't have any known global issues
    }
}