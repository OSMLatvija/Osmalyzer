namespace Osmalyzer;

[UsedImplicitly]
public class JelgavasAutobusuParksStopAnalyzer : PublicTransportStopAnalyzer<JelgavasAutobusuParksAnalysisData>
{
    protected override string Label => "Jelgavas Autobusu Parks";

    protected override string OsmTaggingValueForOperator => "Jelgavas Autobusu Parks";
    protected override string? OsmTaggingValueForOperatorWikidata => null;
    protected override OsmCoord? StopMatchingCenter => new OsmCoord(56.6325, 23.7036); // SW of Jelgava center, because routes go really far to Jēkabnieki
    protected override double? StopMatchingDistance => 14000;
    protected override string GtfsDisambiguatorId => "LV-JAP";
    protected override bool RegionalService => false;
}