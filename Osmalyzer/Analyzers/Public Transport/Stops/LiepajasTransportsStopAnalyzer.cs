namespace Osmalyzer;

[UsedImplicitly]
public class LiepajasTransportsStopAnalyzer : PublicTransportStopAnalyzer<LiepajasTransportsAnalysisData>
{
    protected override string Label => "Liepajas sabiedriskais transports";

    protected override string OsmTaggingValueForOperator => "Liepājas sabiedriskais transports";
    protected override string OsmTaggingValueForOperatorWikidata => "Q141088184";
    protected override OsmCoord? StopMatchingCenter => new OsmCoord(56.5373, 21.0252); // a little north from Liepāja center, routes go far N-S
    protected override double? StopMatchingDistance => 11000;
    protected override string GtfsDisambiguatorId => "LST";
    protected override bool RegionalService => false;
    protected override bool ValidateOsmPickupDropoffValues => false;
}