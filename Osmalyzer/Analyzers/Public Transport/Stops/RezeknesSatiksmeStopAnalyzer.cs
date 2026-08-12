namespace Osmalyzer;

[UsedImplicitly]
public class RezeknesSatiksmeStopAnalyzer : PublicTransportStopAnalyzer<RezeknesSatiksmeAnalysisData>
{
    public override string Name => "Rezeknes Satiksme";

        
    protected override string Label => "RS";

    protected override bool CheckStopBoardingTypes => false;

    protected override string OsmTaggingValueForOperator => "Rēzeknes satiksme";
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
            // Rezeknes almost all stops have "uc" and "nc" like suffixes like "Brīvības iela nc" and "Brīvības iela uc" - probably route direction "no centra"/"uz centru"?
            ptStopName = Regex.Replace(ptStopName, @" (uc|nc|mv)$", @"");

            // todo: move more here from IsStopNameMatchGoodEnough
        
            return ptStopName;
        }
    }
}