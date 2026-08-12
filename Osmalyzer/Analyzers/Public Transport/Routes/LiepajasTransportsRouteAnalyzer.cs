namespace Osmalyzer;

[UsedImplicitly]
public class LiepajasTransportsRouteAnalyzer : PublicTransportRouteAnalyzer<LiepajasTransportsAnalysisData>
{
    public override string Name => "Liepajas Sabiedriskais Transports";


    protected override string Label => "LST";

    protected override string? GtfsDisambiguatorId => null;
    protected override bool RegionalService => false;
    
    
    protected override void CleanUpGtfsData(GTFSNetwork gtfsNetwork)
    {
        // We don't have any known global issues
    }
}