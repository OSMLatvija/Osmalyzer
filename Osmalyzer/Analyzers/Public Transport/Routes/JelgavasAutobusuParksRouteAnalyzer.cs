namespace Osmalyzer;

[UsedImplicitly]
public class JelgavasAutobusuParksRouteAnalyzer : PublicTransportRouteAnalyzer<JelgavasAutobusuParksAnalysisData>
{
    protected override string Label => "Jelgavas autobusu parks";

    protected override string? GtfsDisambiguatorId => null;
    protected override bool RegionalService => false;
    
    protected override string? Agency => null; // we report all as one
    protected override string? AgencyLabel => null; // we report all as one
}