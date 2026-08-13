namespace Osmalyzer;

[UsedImplicitly]
public class RezeknesSatiksmeRouteAnalyzer : PublicTransportRouteAnalyzer<RezeknesSatiksmeAnalysisData>
{
    protected override string Label => "Rezeknes Satiksme";

    protected override string? GtfsDisambiguatorId => null;
    protected override bool RegionalService => false;
}