namespace Osmalyzer;

[UsedImplicitly]
public class AutotransportaDirekcijaRouteAnalyzer : PublicTransportRouteAnalyzer<AutotransportaDirekcijaAnalysisData>
{
    public override string Name => "Autotransporta Direkcija";

        
    protected override string Label => "ATD";

    protected override string? GtfsDisambiguatorId => "LV-ATD";
    protected override bool RegionalService => true;


    protected override void CleanUpGtfsData(GTFSNetwork gtfsNetwork)
    {
        // We don't have any known global issues
    }
}