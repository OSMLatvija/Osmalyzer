namespace Osmalyzer;

[UsedImplicitly]
public class AutotransportaDirekcijaStopAnalyzer : PublicTransportStopAnalyzer<AutotransportaDirekcijaAnalysisData>
{
    public override string Name => "Autotransporta Direkcija";

        
    protected override string Label => "ATD";

    protected override bool CheckStopBoardingTypes => false;

    protected override string OsmTaggingValueForOperator => "Autotransporta Direkcija";
    protected override string? OsmTaggingValueForOperatorWikidata => null;
    protected override OsmCoord? StopMatchingCenter => null;
    protected override double? StopMatchingDistance => null;
    protected override string? GtfsDisambiguatorId => "LV-ATD";
    protected override bool RegionalService => true;
}