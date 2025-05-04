namespace Osmalyzer;

public class GTFSRoute
{
    public string Id { get; }
        
    public string Name { get; }
        
    public string Number { get; }

    public string Type { get; }

    public string CleanType { get; }

    public IEnumerable<GTFSService> Services => _services.AsReadOnly();

    /// <summary>
    /// Trips that point back to this route
    /// </summary>
    public IEnumerable<GTFSTrip> Trips => _trips.AsReadOnly();
    

    private readonly List<GTFSService> _services = [ ];

    private readonly List<GTFSTrip> _trips = [ ];

        
    public GTFSRoute(string id, string name, string number, string type)
    {
        Id = id;
        Name = name;
        Number = number;
        Type = type;
        CleanType = TypeToCleanType(type);
    }


    public void AddService(GTFSService service)
    {
        _services.Add(service);
    }


    public void AddTrip(GTFSTrip trip)
    {
        _trips.Add(trip);
    }

    
    [Pure]
    private static string TypeToCleanType(string type)
    {
        return type switch
        {
            "bus"        => "Bus",
            "trolleybus" => "Trolleybus",
            "tram"       => "Tram",
            "minibus"    => "Minibus",
            _            => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}