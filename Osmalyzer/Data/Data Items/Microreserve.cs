namespace Osmalyzer;

public class Microreserve : IDataItem
{
    public OsmCoord Coord { get; }

    public string Name => throw new NotSupportedException();

    public double Area { get; }


    public Microreserve(OsmCoord coord, double area)
    {
        Coord = coord;
        Area = area;
    }
    
    
    public string ReportString()
    {
        return "Microreserve";
    }


    public override string ToString()
    {
        return "at " + Coord.OsmUrl + " of " + (Area / 1_000_000).ToString("F2") + " km²";
    }
}