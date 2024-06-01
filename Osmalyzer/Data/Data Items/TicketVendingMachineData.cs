namespace Osmalyzer;

public class TicketVendingMachineData : IDataItem
{
    public OsmCoord Coord { get; }
    
    public string? Location { get; }
    
    public string Address { get; }


    public TicketVendingMachineData(OsmCoord coord, string? location, string address)
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