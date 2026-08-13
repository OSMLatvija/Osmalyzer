namespace Osmalyzer;

[UsedImplicitly]
public class AutotransportaDirekcijaRouteAnalyzer : PublicTransportRouteAnalyzer<AutotransportaDirekcijaAnalysisData>
{
    protected override string Label => "Autotransporta Direkcija";

    protected override string? GtfsDisambiguatorId => "LV-ATD";
    protected override bool RegionalService => true;
}