namespace Osmalyzer;

public class LVMPicnicSiteData : IDataItem
{
    public string PicnicSiteName { get; }

    public OsmCoord Coord { get; }


    public LVMPicnicSiteData(string picnicSiteName, OsmCoord coord)
    {
        PicnicSiteName = picnicSiteName;
        Coord = coord;
    }


    public string ReportString()
    {
        return
            PicnicSiteName;
    }
}