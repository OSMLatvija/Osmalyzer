namespace Osmalyzer;

/// <summary>
/// Shown on a map as a line.
/// </summary>
public class MapLineReportEntry : ReportEntry
{
    public List<OsmCoord> Coords { get; }

    public string Color { get; }


    public MapLineReportEntry(IEnumerable<OsmCoord> coords, string text, string color, ReportEntryContext? context = null)
        : base(text, context)
    {
        if (coords == null) throw new ArgumentNullException(nameof(coords));

        Coords = coords.ToList();

        if (Coords.Count < 2)
            throw new ArgumentException("A map line requires at least two coordinates.", nameof(coords));

        Color = color ?? throw new ArgumentNullException(nameof(color));
    }

    public MapLineReportEntry(IEnumerable<OsmCoord> coords, string color, ReportEntryContext? context = null)
        : this(coords, "", color, context) { }
}

