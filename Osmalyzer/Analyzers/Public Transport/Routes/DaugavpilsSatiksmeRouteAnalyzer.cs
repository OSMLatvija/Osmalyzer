namespace Osmalyzer;

[UsedImplicitly]
public class DaugavpilsSatiksmeRouteAnalyzer : PublicTransportRouteAnalyzer<DaugavpilsSatiksmeAnalysisData>
{
    protected override string Label => "Daugavpils satiksme";

    protected override string? GtfsDisambiguatorId => null;
    protected override bool RegionalService => false;

    protected override string? Agency => null; // we report all as one
    protected override string? AgencyLabel => null; // we report all as one

    protected override bool IncludeAllVariants => false;
}

