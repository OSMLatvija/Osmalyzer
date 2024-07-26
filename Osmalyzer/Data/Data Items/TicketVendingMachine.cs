namespace Osmalyzer;

public class TicketVendingMachine : IDataItem
{
    public OsmCoord Coord { get; }
    
    public string? Location { get; }
    
    public string? Address { get; }


    public TicketVendingMachine(OsmCoord coord, string? location, string? address)
    {
        Coord = coord;
        Location = location;
        Address = address;
    }


    public string ReportString()
    {
        return
            Location != null && Address != null ? "`" + Address + "` (`" + Location + "`)"
            : Address != null                   ? "`" + Address + "`"
            : Location != null                  ? "`" + Location + "`"
                                                  : "unspecified location";
    }
}