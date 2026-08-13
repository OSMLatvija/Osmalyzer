namespace Osmalyzer;

[UsedImplicitly]
public class JurmalasSatiksmeRouteAnalyzer : PublicTransportRouteAnalyzer<JurmalasSatiksmeAnalysisData>
{
    protected override string Label => "Jurmalas Autobusu Satiksme";

    protected override string? GtfsDisambiguatorId => null;
    protected override bool RegionalService => false;
}