namespace Osmalyzer;

public class GTFSStopDataItem : IDataItem
{
    public string Modes { get; }

    public GTFSStop Stop { get; }

    
    public OsmCoord Coord => Stop.Coord;

    public string Name => Stop.Name;
        
    
    public GTFSStopDataItem(GTFSStop stop)
    {
        Stop = stop;

        Modes = GetModeString();
    }

    
    [Pure]
    public string ReportString()
    {
        return Modes + " Stop `" + Stop.Name + "` [`" + Stop.Id + "`]";
    }


    [Pure]
    private string GetModeString()
    {
        if (Stop is { Bus: true, Trolleybus: true, Tram: true })
            return "Bus, Trolleybus, Tram";
        if (Stop is { Bus: true, Trolleybus: true })
            return "Bus, Trolleybus";
        if (Stop is { Bus: true, Tram: true })
            return "Bus, Tram";
        if (Stop is { Trolleybus: true, Tram: true })
            return "Trolleybus, Tram";
        if (Stop is { Bus: true })
            return "Bus";
        if (Stop is { Trolleybus: true })
            return "Trolleybus";
        if (Stop is { Tram: true })
            return "Tram";
        
        return "Unvisited";
    }
}

