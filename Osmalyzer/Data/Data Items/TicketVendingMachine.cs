namespace Osmalyzer;

public class TicketVendingMachine : IDataItem
{
    public OsmCoord Coord { get; }
    
    public string? Location { get; }
    
    public string Address { get; }


    public TicketVendingMachine(OsmCoord coord, string? location, string address)
    {
        Coord = coord;
        Location = location;
        Address = address;
    }


    public string ReportString()
    {
        return
            Location != null ?
                "`" + Location + "` (`" + Address + "`)"
                :
                "`" + Address + "`";
    }
}