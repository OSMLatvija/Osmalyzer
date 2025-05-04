namespace Osmalyzer;

/// <summary>
/// "Transit routes. A route is a group of trips that are displayed to riders as a single service."
/// </summary>
public class GTFSRoutes
{
    public IEnumerable<GTFSRoute> Routes => _routes.Values.AsEnumerable();

        
    private readonly Dictionary<string, GTFSRoute> _routes;

        
    public GTFSRoutes(string dataFileName)
    {
        string[] lines = File.ReadAllLines(dataFileName);

        _routes = new Dictionary<string, GTFSRoute>();

        int idIndex = 0;
        int shortNameIndex = 0;
        int longNameIndex = 0;
            
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            if (i == 0) // header row
            {
                List<string> headerSegments = line.Split(',').Select(s => s.Trim()).ToList();

                idIndex = headerSegments.FindIndex(s => s == "route_id");
                shortNameIndex = headerSegments.FindIndex(s => s == "route_short_name");
                longNameIndex = headerSegments.FindIndex(s => s == "route_long_name");
                    
                continue;
            }

            // route_id,route_short_name,route_long_name,route_desc,route_type,route_url,route_color,route_text_color,route_sort_order
            // riga_bus_3,"3","Daugavgrīva - Pļavnieki",,3,https://saraksti.rigassatiksme.lv/index.html#riga/bus/3,F4B427,FFFFFF,2000300
                
            // route_id, agency_id, route_short_name, route_long_name, route_desc, route_type, route_url, route_color, route_text_color
            // 5800064, 58, 6079, "Ventspils-Ziras", , 3, , , 
            
            // route_id,agency_id,route_short_name,route_long_name,route_desc,route_type,route_url,route_color,route_text_color
            // JAP:Line:1,JAP:Operator:JAP,1,Tušķi GP - Ozolnieku vidusskola,,3,,4A55A0,000000

            List<string> segments = line.Split(',').Select(s => s.Trim()).ToList();

            string id = segments[idIndex];
            string name = GetName(segments, longNameIndex);
            string number = GetName(segments, shortNameIndex);

            string type = TypeFromId(id);
                
            GTFSRoute route = new GTFSRoute(id, name, number, type);

            _routes.TryAdd(route.Id, route);
        }

        return;

        
        [Pure]
        static string GetName(List<string> segments, int i)
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
    private static string TypeFromId(string id)
    {
        string[] split = id.Split('_');

        if (split.Length == 1)
            return "bus";
            
        string rawType = split[1];
        // riga_bus_60
        // riga_tram_5
        // riga_trol_16

        return rawType switch
        {
            "bus"     => "bus",
            "trol"    => "trolleybus",
            "tram"    => "tram",
            "minibus" => "minibus",
            _         => throw new ArgumentOutOfRangeException()
        };
    }
}