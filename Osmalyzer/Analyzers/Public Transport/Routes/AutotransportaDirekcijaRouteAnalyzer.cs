namespace Osmalyzer;

[UsedImplicitly]
public class AutotransportaDirekcijaRouteAnalyzer : PublicTransportRouteAnalyzer<AutotransportaDirekcijaAnalysisData>
{
    protected override string Label => "Autotransporta Direkcija";

    protected override string? GtfsDisambiguatorId => "LV-ATD";
    protected override bool RegionalService => true;


    protected override void CleanUpGtfsData(GTFSNetwork gtfsNetwork)
    {
        // We don't have any known global issues
    }
}