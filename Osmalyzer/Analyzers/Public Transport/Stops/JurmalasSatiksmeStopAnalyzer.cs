namespace Osmalyzer;

[UsedImplicitly]
public class JurmalasSatiksmeStopAnalyzer : PublicTransportStopAnalyzer<JurmalasSatiksmeAnalysisData>
{
    public override string Name => "Jurmalas Autobusu Satiksme";

        
    protected override string Label => "JS";

    protected override bool CheckStopBoardingTypes => false;

    protected override string OsmTaggingValueForOperator => "Jūrmalas Autobusu Satiksme";
    protected override string? OsmTaggingValueForOperatorWikidata => null;
    protected override OsmCoord? StopMatchingCenter => null;
    protected override double? StopMatchingDistance => null;
    protected override string? GtfsDisambiguatorId => null;
    protected override bool RegionalService => false;
    
    
    protected override void CleanUpGtfsData(GTFSNetwork gtfsNetwork)
    {
        // We don't have any known global issues
    }
}