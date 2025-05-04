namespace Osmalyzer;

[UsedImplicitly]
public class JelgavasAutobusuParksAnalyzer : PublicTransportAnalyzer<JelgavasAutobusuParksAnalysisData>
{
    public override string Name => "Jelgavas Autobusu Parks";

        
    protected override string Label => "JAP";
    
    
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