namespace Osmalyzer;

public class GTFSStop
{
    public string Id { get; }

    public string Name { get; private set; }
        
    public OsmCoord Coord { get; }
    
    public GTFSStopLocationType LocationType { get; }

    
    public bool Bus { get; set; }
    
    public bool Minibus { get; set; }
        
    public bool Trolleybus { get; set; }
        
    public bool Tram { get; set; }
    
    public GTFSPickupType CommonPickupType { get; set; }
    
    public GTFSDropoffType CommonDropOffType { get; set; }
    
    
    public IEnumerable<GTFSVisit> Visits => _visits.AsReadOnly();


    private readonly List<GTFSVisit> _visits = [ ];


    public GTFSStop(string id, string name, OsmCoord coord, GTFSStopLocationType locationType)
    {
        Id = id;
        Name = name;
        Coord = coord;
        LocationType = locationType;
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


public enum GTFSStopLocationType
{
    Invalid,
    
    /// <summary> Can generally be assumed to be a <see cref="Stop"/> </summary>
    Unknown,
    
    Stop,
    Station,
    EntranceExit,
    GenericNode,
    BoardingArea
}