namespace Osmalyzer;

[UsedImplicitly]
public class JelgavasAutobusuParksRouteAnalyzer : PublicTransportRouteAnalyzer<JelgavasAutobusuParksAnalysisData>
{
    protected override string Label => "Jelgavas autobusu parks";

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