namespace Osmalyzer;

public class StatePoliceData : IDataItem
{
    public string Name { get; }
    
    public OsmCoord Coord { get; }
    
    public string ShortName => Name; // TODO:


    public StatePoliceData(string officeName, OsmCoord coord)
    {
        Coord = coord;
        Name = officeName;
    }
    
    
    public string ReportString()
    {
        return Name;
    }
}