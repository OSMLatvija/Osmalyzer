namespace Osmalyzer;

[UsedImplicitly]
public class JelgavasAutobusuParksStopAnalyzer : PublicTransportStopAnalyzer<JelgavasAutobusuParksAnalysisData>
{
    protected override string Label => "Jelgavas autobusu parks";

    protected override string OsmTaggingValueForOperator => "Jelgavas autobusu parks";
    protected override string OsmTaggingValueForOperatorWikidata => "Q141045544";
    protected override OsmCoord? StopMatchingCenter => new OsmCoord(56.6325, 23.7036); // SW of Jelgava center, because routes go really far to Jēkabnieki
    protected override double? StopMatchingDistance => 14000;
    protected override string GtfsDisambiguatorId => "LV-JAP";
    protected override bool RegionalService => false;
}