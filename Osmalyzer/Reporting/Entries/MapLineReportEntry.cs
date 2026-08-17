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

    public MapLineReportEntry(IEnumerable<OsmCoord> coords, string text, List<OsmElement> elements, string color, ReportEntryContext? context = null)
        : this(coords, AddElementTagsToText(text, elements), color, context) { }

    public MapLineReportEntry(IEnumerable<OsmCoord> coords, string text, OsmElement element, string color, ReportEntryContext? context = null)
        : this(coords, AddElementTagsToText(text, [ element ]), color, context) { }


    [Pure]
    private static string AddElementTagsToText(string text, List<OsmElement> elements)
    {
        foreach (OsmElement element in elements)
            text += OsmElementTagsBlock(element);

        return text;


        [Pure]
        static string? OsmElementTagsBlock(OsmElement osmElement)
        {
            string? tags = osmElement.GetAllTagsAsString();

            return tags != null ? Environment.NewLine + "```" + osmElement.ElementType + " #" + osmElement.Id + Environment.NewLine + tags + "```" : null;
        }
    }
}

