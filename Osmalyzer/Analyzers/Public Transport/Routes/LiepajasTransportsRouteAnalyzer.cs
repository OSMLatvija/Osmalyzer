namespace Osmalyzer;

[UsedImplicitly]
public class LiepajasTransportsRouteAnalyzer : PublicTransportRouteAnalyzer<LiepajasTransportsAnalysisData>
{
    protected override string Label => "Liepajas sabiedriskais transports";

    protected override string? GtfsDisambiguatorId => null;
    protected override bool RegionalService => false;
    
    protected override string? Agency => null; // we report all as one
    protected override string? AgencyLabel => null; // we report all as one
    
    protected override bool IncludeAllVariants => false;
}