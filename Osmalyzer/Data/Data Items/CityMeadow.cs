namespace Osmalyzer;

public class CityMeadow : IDataItem
{
    public OsmCoord Coord { get; }

    public string Name { get; }

    public int StartYear { get; }


    public CityMeadow(OsmCoord coord, string name, int startYear)
    {
        Coord = coord;
        Name = name;
        StartYear = startYear;
    }

        
    public string ReportString()
    {
        return
            "City meadow " +
            "`" + Name + "` " +
            "(" + StartYear + ")";
    }
}