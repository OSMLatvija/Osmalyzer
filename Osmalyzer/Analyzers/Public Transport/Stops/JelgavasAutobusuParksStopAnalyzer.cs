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
    
    
    protected override void CleanUpGtfsData(GTFSNetwork gtfsNetwork)
    {
        gtfsNetwork.CleanStopNames(CleanRouteStopName);

        [Pure]
        static string CleanRouteStopName(string ptStopName)
        {
            // Jelgavas AP has depot/terminal stops with "GP" suffix like "Tušķi GP"
            ptStopName = Regex.Replace(ptStopName, @" GP$", @"");

            // todo: move more here from IsStopNameMatchGoodEnough
        
            return ptStopName;
        }
    }
}