namespace Osmalyzer;

public class GTFSStop
{
    public string Id { get; }

    public string Name { get; private set; }
        
    public OsmCoord Coord { get; }
        
    public bool Bus { get; set; }
        
    public bool Trolleybus { get; set; }
        
    public bool Tram { get; set; }


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
}