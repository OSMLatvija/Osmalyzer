namespace Osmalyzer;

[UsedImplicitly]
public class LiepajasTransportsRouteAnalyzer : PublicTransportRouteAnalyzer<LiepajasTransportsAnalysisData>
{
    protected override string Label => "Liepajas Sabiedriskais Transports";

    protected override string? GtfsDisambiguatorId => null;
    protected override bool RegionalService => false;
    
    
    protected override void CleanUpGtfsData(GTFSNetwork gtfsNetwork)
    {
        // We don't have any known global issues
    }
}