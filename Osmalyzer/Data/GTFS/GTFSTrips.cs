namespace Osmalyzer;

/// <summary>
/// "Trips for each route. A trip is a sequence of two or more stops that occur during a specific time period."
/// </summary>
public class GTFSTrips
{
    public IEnumerable<GTFSTrip> Trips => _trips.Values.AsEnumerable();

        
    private readonly Dictionary<string, GTFSTrip> _trips;

        
    public GTFSTrips(string dataFileName, GTFSRoutes routes, GTFSServices services, GTFSShapes? shapes)
    {
        _trips = new Dictionary<string, GTFSTrip>(StringComparer.OrdinalIgnoreCase);

        string[] lines = File.ReadAllLines(dataFileName);

        if (lines.Length < 2)
            return;

        string header = lines[0];
        string[] headers = header.Split(',').Select(s => s.Trim()).ToArray();

        int routeIdIndex = headers.IndexOf("route_id");
        int serviceIdIndex = headers.IndexOf("service_id");
        int tripIdIndex = headers.IndexOf("trip_id");
        int shapeIdIndex = headers.IndexOf("shape_id");

        if (routeIdIndex == -1) throw new Exception($"Missing required column 'route_id' in {dataFileName}");
        if (serviceIdIndex == -1) throw new Exception($"Missing required column 'service_id' in {dataFileName}");
        if (tripIdIndex == -1) throw new Exception($"Missing required column 'trip_id' in {dataFileName}");

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            List<string> segments = line.Split(',').Select(s => s.Trim()).ToList();

            if (segments.Count <= routeIdIndex || segments.Count <= serviceIdIndex || segments.Count <= tripIdIndex)
                continue;

            // route_id,service_id,trip_id,trip_headsign,direction_id,block_id,shape_id,wheelchair_accessible
            // riga_bus_9,23274,1279,"Abrenes iela",1,169766,riga_bus_9_b-a,

            string tripId = segments[tripIdIndex];
            string routeId = segments[routeIdIndex];
            string serviceId = segments[serviceIdIndex];

            GTFSRoute route = routes.GetRoute(routeId);

            GTFSService? service = services.GetService(serviceId);

            GTFSShape? shape = null;
            if (shapeIdIndex != -1 && shapeIdIndex < segments.Count)
                shape = shapes?.GetShape(segments[shapeIdIndex]);

            GTFSTrip trip = new GTFSTrip(tripId, route, service, shape);
            _trips.TryAdd(trip.Id, trip);
            
            route.AddTrip(trip);

            if (service != null)
            {
                service.AddTrip(trip);

                // Add the service to the route (if it doesn't already know about it)
                // Service may be used for several routes, i.e. different bus numbers do the same service on different days/times or something
                // And vice-verse - add route to service (if not known)

                if (route.Services.All(s => s != service))
                    route.AddService(service);

                if (service.Routes.All(r => r != route))
                    service.AddRoute(route);
            }

            if (shape != null)
                shape.AddTrip(trip);
        }
    }

        
    [Pure]
    public GTFSTrip? GetTrip(string id)
    {
        if (_trips.TryGetValue(id, out GTFSTrip? s))
            return s;
            
        return null;
    }
}