namespace Osmalyzer;

[UsedImplicitly]
public class AutotransportaDirekcijaStopAnalyzer : PublicTransportStopAnalyzer<AutotransportaDirekcijaAnalysisData>
{
    protected override string Label => "Autotransporta Direkcija";

    protected override string OsmTaggingValueForOperator => "Autotransporta Direkcija";
    protected override string? OsmTaggingValueForOperatorWikidata => null;
    protected override OsmCoord? StopMatchingCenter => null;
    protected override double? StopMatchingDistance => null;
    protected override string? GtfsDisambiguatorId => "LV-ATD";
    protected override bool RegionalService => true;
    protected override bool ValidateOsmPickupDropoffValues => false;
}