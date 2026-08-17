namespace Osmalyzer;

/// <summary>
/// Shown on a map as a line.
/// </summary>
public class MapLineReportEntry : ReportEntry
{
    public List<OsmCoord> Coords { get; }

    public string Color { get; }


    public MapLineReportEntry(IEnumerable<OsmCoord> coords, string color, ReportEntryContext? context = null)
        : base("", context)
    {
        if (coords == null) throw new ArgumentNullException(nameof(coords));

        Coords = coords.ToList();

        if (Coords.Count < 2)
            throw new ArgumentException("A map line requires at least two coordinates.", nameof(coords));

        Color = color ?? throw new ArgumentNullException(nameof(color));
    }


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

