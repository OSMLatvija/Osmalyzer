namespace Osmalyzer;

[UsedImplicitly]
public class AutotransportaDirekcijaAnalyzer : PublicTransportAnalyzer<AutotransportaDirekcijaAnalysisData>
{
    public override string Name => "Autotransporta Direkcija";

        
    protected override string Label => "ATD";

    protected override bool CheckStopBoardingTypes => false;

    protected override string OsmTaggingValueForOperator => "Autotransporta Direkcija";
    protected override string? OsmTaggingValueForOperatorWikidata => null;
    protected override OsmCoord? StopMatchingCenter => null;
    protected override double? StopMatchingDistance => null;
    protected override string? GtfsDisambiguatorId => "LV-ATD";


    protected override void CleanUpGtfsData(GTFSNetwork gtfsNetwork)
    {
        // We don't have any known global issues
    }
}