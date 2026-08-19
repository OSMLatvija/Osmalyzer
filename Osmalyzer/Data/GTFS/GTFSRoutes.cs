namespace Osmalyzer;

/// <summary>
/// "Transit routes. A route is a group of trips that are displayed to riders as a single service."
/// </summary>
public class GTFSRoutes
{
    public IEnumerable<GTFSRoute> Routes => _routes.Values.AsEnumerable();

    
    private readonly Dictionary<string, GTFSRoute> _routes;


    public GTFSRoutes(string dataFileName, GTFSAgencies? agencies)
    {
        string[] lines = File.ReadAllLines(dataFileName);

        _routes = new Dictionary<string, GTFSRoute>(StringComparer.OrdinalIgnoreCase);

        if (lines.Length < 2)
            return;

        string[] headers = lines[0].Split(',').Select(s => s.Trim()).ToArray();

        int routeIdIndex = headers.IndexOf("route_id");
        int agencyIdIndex = headers.IndexOf("agency_id");
        int shortNameIndex = headers.IndexOf("route_short_name");
        int longNameIndex = headers.IndexOf("route_long_name");

        if (routeIdIndex == -1) throw new Exception($"Missing required column 'route_id' in {dataFileName}");

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            List<string> segments = line.Split(',').Select(s => s.Trim()).ToList();

            if (segments.Count <= routeIdIndex)
                continue;

            string id = GetValue(segments, routeIdIndex);
            if (string.IsNullOrWhiteSpace(id))
                continue;

            string number = shortNameIndex != -1 && shortNameIndex < segments.Count ? GetValue(segments, shortNameIndex) : "";
            string name = longNameIndex != -1 && longNameIndex < segments.Count ? GetValue(segments, longNameIndex) : number;

            if (string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(number))
                name = number;

            GTFSAgency? agency = null;
            if (agencies != null && agencyIdIndex != -1 && agencyIdIndex < segments.Count)
            {
                string agencyId = GetValue(segments, agencyIdIndex);
                if (!string.IsNullOrWhiteSpace(agencyId))
                    agency = agencies.GetAgency(agencyId);
            }

            GTFSRouteVehicleType type = TypeFromId(id);

            GTFSRoute route = new GTFSRoute(id, name, number, type, agency);
            _routes.TryAdd(route.Id, route);

            agency?.AddRoute(route);
        }

        return;

        [Pure]
        static string GetValue(List<string> segments, int i)
        {
            string segment = segments[i];

            if (segment.Length <= 2) return segment;

            if (segment.StartsWith('"') && segment.EndsWith('"'))
                return segment.Substring(1, segment.Length - 2).Replace("\"\"", "\"");

            return segment;
        }
    }

    
    [Pure]
    public GTFSRoute GetRoute(string id)
    {
        return _routes[id];
    }


    [Pure]
    private static GTFSRouteVehicleType TypeFromId(string id)
    {
        string[] split = id.Split('_');

        if (split.Length == 1)
            return GTFSRouteVehicleType.Bus; // Default to bus if no type found

        string rawType = split[1];
        // riga_bus_60
        // riga_tram_5
        // riga_trol_16

        return rawType switch
        {
            "bus"      => GTFSRouteVehicleType.Bus,
            "nightbus" => GTFSRouteVehicleType.NightBus,
            "trol"     => GTFSRouteVehicleType.Trolleybus,
            "tram"     => GTFSRouteVehicleType.Tram,
            "minibus"  => GTFSRouteVehicleType.Minibus,
            _          => throw new ArgumentOutOfRangeException()
        };
    }
}