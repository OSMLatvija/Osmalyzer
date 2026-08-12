namespace Osmalyzer;

[UsedImplicitly]
public class RezeknesSatiksmeRouteAnalyzer : PublicTransportRouteAnalyzer<RezeknesSatiksmeAnalysisData>
{
    protected override string Label => "Rezeknes Satiksme";

    protected override string? GtfsDisambiguatorId => null;
    protected override bool RegionalService => false;


    protected override void CleanUpGtfsData(GTFSNetwork gtfsNetwork)
    {
        gtfsNetwork.CleanStopNames(CleanRouteStopName);

        [Pure]
        static string CleanRouteStopName(string ptStopName)
        {
            // Rezeknes almost all stops have "uc" and "nc" like suffixes like "Brīvības iela nc" and "Brīvības iela uc" - probably route direction "no centra"/"uz centru"?
            ptStopName = Regex.Replace(ptStopName, @" (uc|nc|mv)$", @"");

            // todo: move more here from IsStopNameMatchGoodEnough
        
            return ptStopName;
        }
    }
}