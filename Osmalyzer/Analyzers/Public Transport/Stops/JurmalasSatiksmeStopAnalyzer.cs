namespace Osmalyzer;

[UsedImplicitly]
public class JurmalasSatiksmeStopAnalyzer : PublicTransportStopAnalyzer<JurmalasSatiksmeAnalysisData>
{
    protected override string Label => "Jurmalas Autobusu Satiksme";

    protected override bool CheckStopBoardingTypes => false;

    protected override string OsmTaggingValueForOperator => "Jūrmalas Autobusu Satiksme";
    protected override string? OsmTaggingValueForOperatorWikidata => null;
    protected override OsmCoord? StopMatchingCenter => null;
    protected override double? StopMatchingDistance => null;
    protected override string? GtfsDisambiguatorId => null;
    protected override bool RegionalService => false;
}