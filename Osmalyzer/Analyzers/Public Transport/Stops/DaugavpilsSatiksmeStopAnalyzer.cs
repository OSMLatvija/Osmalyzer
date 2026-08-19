namespace Osmalyzer;

[UsedImplicitly]
public class DaugavpilsSatiksmeStopAnalyzer : PublicTransportStopAnalyzer<DaugavpilsSatiksmeAnalysisData>
{
    protected override string Label => "Daugavpils satiksme";

    protected override string OsmTaggingValueForOperator => "Daugavpils satiksme";
    protected override string? OsmTaggingValueForOperatorWikidata => null;
    protected override OsmCoord? StopMatchingCenter => new OsmCoord(55.8833, 26.5236); // Daugavpils center
    protected override double? StopMatchingDistance => 12000;
    protected override string? GtfsDisambiguatorId => null;
    protected override bool RegionalService => false;
    protected override bool ValidateOsmPickupDropoffValues => false;
}

