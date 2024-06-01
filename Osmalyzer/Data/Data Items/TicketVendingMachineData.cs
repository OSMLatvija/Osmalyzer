namespace Osmalyzer;

public class TicketVendingMachineData : IDataItem
{
    public OsmCoord Coord { get; }
    
    public string Name { get; }
    
    public string Address { get; }


    public TicketVendingMachineData(OsmCoord coord, string name, string address)
    {
        Coord = coord;
        Name = name;
        Address = address;
    }


    public string ReportString()
    {
        return "`" + Name + "` (`" + Address + "`)";
    }
}