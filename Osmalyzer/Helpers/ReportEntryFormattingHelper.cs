namespace Osmalyzer;

/// <summary>
/// Common utilities for doing report stuff in analyzers.
/// </summary>
public static class ReportEntryFormattingHelper
{
    public static object ListElements(IEnumerable<OsmElement> elements, int maxShown = 5)
    {
        List<OsmElement> list = elements.ToList();

        if (list.Count > maxShown)
        {
            return string.Join("; ", list.Take(maxShown - 1).Select(e => e.OsmViewUrl)) + " and " + (list.Count - maxShown + 1) + " more";
            // We assume the space for the "and X more" occupies roughly the space to draw the element, so we draw 1 less than max (also "and 1 more" is stupid - and we may as well have shown it then) 
        }
        else
        {
            return string.Join("; ", list.Select(e => e.OsmViewUrl));
        }
    }
}