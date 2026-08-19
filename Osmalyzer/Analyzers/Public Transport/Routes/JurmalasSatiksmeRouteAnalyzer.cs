namespace Osmalyzer;

[UsedImplicitly]
public class JurmalasSatiksmeRouteAnalyzer : PublicTransportRouteAnalyzer<JurmalasSatiksmeAnalysisData>
{
    protected override string Label => "Jurmalas Autobusu Satiksme";

    protected override string? GtfsDisambiguatorId => null;
    protected override bool RegionalService => false;
    
    protected override string? Agency => null; // we report all as one
    protected override string? AgencyLabel => null; // we report all as one
    
    protected override bool IncludeAllVariants => false;
}