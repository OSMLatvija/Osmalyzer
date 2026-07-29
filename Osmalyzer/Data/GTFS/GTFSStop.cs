namespace Osmalyzer;

public class GTFSStop
{
    public string Id { get; }

    public string Name { get; private set; }
        
    public OsmCoord Coord { get; }
        
    public bool Bus { get; set; }
        
    public bool Trolleybus { get; set; }
        
    public bool Tram { get; set; }
    
    public int? CommonPickupType { get; set; }
    
    public int? CommonDropOffType { get; set; }
    
    
    public IEnumerable<GTFSVisit> Visits => _visits.AsReadOnly();


    private readonly List<GTFSVisit> _visits = [ ];


    public GTFSStop(string id, string name, OsmCoord coord)
    {
        Id = id;
        Name = name;
        Coord = coord;
    }

    
    public void CleanName(Func<string, string> callback)
    {
        Name = callback(Name);
    }

    public void AddPoint(GTFSVisit visit)
    {
        _visits.Add(visit);
    }
}