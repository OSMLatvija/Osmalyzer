namespace Osmalyzer;

[UsedImplicitly]
public class JelgavasAutobusuParksStopAnalyzer : PublicTransportStopAnalyzer<JelgavasAutobusuParksAnalysisData>
{
    public override string Name => "Jelgavas Autobusu Parks";

        
    protected override string Label => "JAP";

    protected override bool CheckStopBoardingTypes => false;

    protected override string OsmTaggingValueForOperator => "Jelgavas Autobusu Parks";
    protected override string? OsmTaggingValueForOperatorWikidata => null;
    protected override OsmCoord? StopMatchingCenter => null;
    protected override double? StopMatchingDistance => null;
    protected override string? GtfsDisambiguatorId => null;
    protected override bool RegionalService => false;
}