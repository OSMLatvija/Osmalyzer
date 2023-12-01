namespace Osmalyzer;

public class DrinkingWater : IDataItem
{
    public string Name { get; }
        
    public InstallationType Type { get; }
        
    public OsmCoord Coord { get; }


    public DrinkingWater(string name, InstallationType type, OsmCoord coord)
    {
        Name = name;
        Type = type;
        Coord = coord;
    }
        
        
    public string ReportString()
    {
        return
            "Riga tap " +
            "`" + Name + "`";
    }


    public enum InstallationType
    {
        Static,
        Mobile
    }
}