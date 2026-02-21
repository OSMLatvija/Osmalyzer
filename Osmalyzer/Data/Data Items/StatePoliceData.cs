namespace Osmalyzer;

public class StatePoliceData : IDataItem
{
    public string OfficeName { get; }

    public string Name => OfficeName;
    
    public OsmCoord Coord { get; }


    public StatePoliceData(string officeName, OsmCoord coord)
    {
        Coord = coord;
        OfficeName = officeName;
    }
    
    
    public string ReportString()
    {
        return OfficeName;
    }
}