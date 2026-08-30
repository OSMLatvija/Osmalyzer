namespace Osmalyzer;

[UsedImplicitly]
public class LiepajasTransportsAnalysisData : GTFSAnalysisData
{
    public override string Name => "Liepajas sabiedriskais transports";

    public override string ReportWebLink => @"https://www.marsruti.lv/liepaja/";


    public override bool DataDateHasDayGranularity => true; 

    protected override string DataFileIdentifier => "liepajas-transports";


    protected override string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".zip");

    public override string ExtractionFolder => "GTFS-LiepTra";
        
    protected override string DataURL => @"https://marsruti.lv/liepaja/liepaja/gtfs.zip";
    // todo: where could I get a direct link from them? 
    
    
    protected override void CleanUpGtfsData()
    {
        // LST routes with numbers larger than 20 are minibuses, not regular buses (22, 23, 25, 25A at this time while largest bus number is 10)
        foreach (GTFSRoute route in Network.Routes.Routes)
            if (TryParseRouteNumber(route.Number, out int? routeNumber))
                if (routeNumber > 20)
                    route.Type = GTFSRouteVehicleType.Minibus;
    }

    /// <summary>
    /// Extract the numeric part from a route number (e.g., "20A" -> 20, "123" -> 123).
    /// </summary>
    [Pure]
    private static bool TryParseRouteNumber(string routeNumber, out int? number)
    {
        number = null;
        
        Match match = Regex.Match(routeNumber, @"^(\d+)");
        
        if (!match.Success)
            return false;

        if (!int.TryParse(match.Groups[1].Value, out int parsed))
            return false;

        number = parsed;
        return true;
    }
}