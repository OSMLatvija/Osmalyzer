namespace Osmalyzer;

[UsedImplicitly]
public class RezeknesSatiksmeStopAnalyzer : PublicTransportStopAnalyzer<RezeknesSatiksmeAnalysisData>
{
    protected override string Label => "Rezeknes Satiksme";

    protected override string OsmTaggingValueForOperator => "Rēzeknes satiksme";
    protected override string? OsmTaggingValueForOperatorWikidata => null;
    protected override OsmCoord? StopMatchingCenter => new OsmCoord(56.5077, 27.3289); // almost Rēzekne center, routes go far N, S and E
    protected override double? StopMatchingDistance => 12000;
    protected override string GtfsDisambiguatorId => "RZS"; // TODO: I made this up, because RS ir Rīgas Satiksme
    protected override bool RegionalService => false;
}