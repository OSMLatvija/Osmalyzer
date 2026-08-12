namespace Osmalyzer;

[UsedImplicitly]
public class RezeknesSatiksmeStopAnalyzer : PublicTransportStopAnalyzer<RezeknesSatiksmeAnalysisData>
{
    protected override string Label => "Rezeknes Satiksme";

    protected override bool CheckStopBoardingTypes => false;

    protected override string OsmTaggingValueForOperator => "Rēzeknes satiksme";
    protected override string? OsmTaggingValueForOperatorWikidata => null;
    protected override OsmCoord? StopMatchingCenter => null;
    protected override double? StopMatchingDistance => null;
    protected override string? GtfsDisambiguatorId => null;
    protected override bool RegionalService => false;
}