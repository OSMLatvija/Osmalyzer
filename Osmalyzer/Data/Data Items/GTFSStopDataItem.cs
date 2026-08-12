namespace Osmalyzer;

public class GTFSStopDataItem : IDataItem
{
    public string Modes { get; }

    public GTFSStop Stop { get; }


    /// <summary> GTFS doesn't specify, but an existing route or something else implies </summary>
    public bool ImpliedBus { get; set; }
    
    /// <summary> GTFS doesn't specify, but an existing route or something else implies </summary>
    public bool ImpliedTrolleybus { get; set; }
    
    /// <summary> GTFS doesn't specify, but an existing route or something else implies </summary>
    public bool ImpliedTram { get; set; }


    public OsmCoord Coord => Stop.Coord;

    public string Name => Stop.Name;

    public bool Bus => Stop.Bus || ImpliedBus;
    public bool Trolleybus => Stop.Trolleybus || ImpliedTrolleybus;
    public bool Tram => Stop.Tram || ImpliedTram;


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
        string modes = "Unvisited";
        
        if (Bus && Trolleybus && Tram)
            modes = "Bus, Trolleybus, Tram";
        if (Bus && Trolleybus)
            modes = "Bus, Trolleybus";
        if (Bus && Tram)
            modes = "Bus, Tram";
        if (Trolleybus && Tram)
            modes = "Trolleybus, Tram";
        if (Bus)
            modes = "Bus";
        if (Trolleybus)
            modes = "Trolleybus";
        if (Tram)
            modes = "Tram";
        
        if (ImpliedBus && !Stop.Bus) 
            modes = modes.Replace("Bus", "Bus ∴");
        if (ImpliedTrolleybus && !Stop.Trolleybus)
            modes = modes.Replace("Trolleybus", "Trolleybus ∴");
        if (ImpliedTram && !Stop.Tram)
            modes = modes.Replace("Tram", "Tram ∴");
        
        return modes;
    }
}

