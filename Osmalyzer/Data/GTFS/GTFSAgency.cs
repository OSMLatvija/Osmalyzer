namespace Osmalyzer;

public class GTFSAgency
{
    public string Id { get; }

    public string Name { get; }

    public string Url { get; }

    public string Timezone { get; }

    public string? Language { get; }

    public string? Phone { get; }

    public string? FareUrl { get; }

    public string? Email { get; }

    
    public IEnumerable<GTFSRoute> Routes => _routes.AsReadOnly();

    
    private readonly List<GTFSRoute> _routes = [ ];

    
    public GTFSAgency(string id, string name, string url, string timezone, string? language = null, string? phone = null, string? fareUrl = null, string? email = null)
    {
        Id = id;
        Name = name;
        Url = url;
        Timezone = timezone;
        Language = language;
        Phone = phone;
        FareUrl = fareUrl;
        Email = email;
    }

    
    public void AddRoute(GTFSRoute route)
    {
        if (!_routes.Contains(route))
            _routes.Add(route);
    }

    
    public override string ToString()
    {
        return $"{Name} ({Id})";
    }
}

