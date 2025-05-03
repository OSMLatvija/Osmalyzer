namespace Osmalyzer;

public class GTFSRoute
{
    public string Id { get; }
        
    public string Name { get; }
        
    public string Number { get; }

    public string Type { get; }

    public string CleanType { get; }

    public IEnumerable<GTFSService> Services => _services.AsReadOnly();

    /// <summary> Trips (unique) from all <see cref="Services"/> </summary>
    public IEnumerable<GTFSTrip> AllTrips => _services.SelectMany(service => service.Trips).Distinct();


    private readonly List<GTFSService> _services = new List<GTFSService>();

        
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