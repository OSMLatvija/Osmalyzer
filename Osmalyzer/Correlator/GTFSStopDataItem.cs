namespace Osmalyzer;

/// <summary>
/// Wraps a GTFS stop as an IDataItem for use with the Correlator.
/// </summary>
public record GTFSStopDataItem(GTFSStop Stop) : IDataItem
{
    public OsmCoord Coord => Stop.Coord;

    public string? Name => Stop.Name;

    [Pure]
    public string ReportString()
    {
        return "Stop `" + Stop.Name + "` [`" + Stop.Id + "`]";
    }
}

