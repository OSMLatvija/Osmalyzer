namespace Osmalyzer;

[UsedImplicitly]
public class LiepajasTransportsRouteAnalyzer : PublicTransportRouteAnalyzer<LiepajasTransportsAnalysisData>
{
    protected override string Label => "Liepajas sabiedriskais transports";

    protected override string? GtfsDisambiguatorId => null;
    protected override bool RegionalService => false;
}