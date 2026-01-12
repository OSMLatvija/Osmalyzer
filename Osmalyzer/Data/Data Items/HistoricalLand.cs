namespace Osmalyzer;

public class HistoricalLand : IDataItem, IHasCspPopulationEntry
{
    public string Id { get; }

    public string Code { get; }

    public string Name { get; }
    
    public OsmCoord Coord { get; }
    
    public OsmMultiPolygon Boundary { get; }

    public CspPopulationEntry? CspPopulationEntry { get; set; }


    public HistoricalLand(string id, string code, string name, OsmCoord coord, OsmMultiPolygon boundary)
    {
        Id = id;
        Code = code;
        Name = name;
        Coord = coord;
        Boundary = boundary;
    }
    
    
    public string ReportString()
    {
        return 
            "Historical Land" + 
            " `" + Name + "`" +
            " #`" + Id + "`";
    }

    public override string ToString() => ReportString();
}
