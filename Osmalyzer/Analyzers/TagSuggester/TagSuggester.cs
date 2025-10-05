namespace Osmalyzer;

/// <summary>
/// Instance-based tag suggester for matched correlations; compares configured data fields to OSM tags
/// and emits report entries for missing or different values.
/// </summary>
public class TagSuggester<TDataItem>
    where TDataItem : IDataItem
{
    private readonly IEnumerable<MatchedCorrelation<TDataItem>> _matchedPairs;
    
    private readonly Func<TDataItem, string> _subjectNameSelector;
    
    private readonly string _subjectTypeLabel;
    

    
    public TagSuggester(
        IEnumerable<MatchedCorrelation<TDataItem>> matchedPairs,
        Func<TDataItem, string> subjectNameSelector,
        string subjectTypeLabel)
    {
        _matchedPairs = matchedPairs;
        _subjectNameSelector = subjectNameSelector;
        _subjectTypeLabel = subjectTypeLabel;
    }

    
    /// <summary>
    /// Iterate through matched pairs and write suggested updates to the report based on provided comparisons.
    /// Creates the report group internally so analyzers don't have to.
    /// </summary>
    public void Suggest(Report report, IEnumerable<TagComparison<TDataItem>> comparisons)
    {
        if (!_matchedPairs.Any())
            return;

        // Prepare report group lazily (only once)
        report.AddGroup(
            ReportGroup.TagSuggestions,
            "Suggested Updates",
            "These matched items have missing or mismatched tags compared to parsed source data. " +
            "Note that source data is not guaranteed to be correct and parsing is not guaranteed to have correct OSM values."
        );

        foreach (MatchedCorrelation<TDataItem> pair in _matchedPairs)
        {
            OsmElement osmElement = pair.OsmElement;
            TDataItem item = pair.DataItem;

            string subjectName = _subjectNameSelector(item);

            foreach (TagComparison<TDataItem> comparison in comparisons)
            {
                string? expected = comparison.ExpectedValueSelector(item);

                if (string.IsNullOrWhiteSpace(expected))
                    continue; // no expectation for this item

                string tag = comparison.OsmKey;
                string? actual = osmElement.GetValue(tag);

                if (actual == null)
                {
                    AddMissing(report, subjectName, tag, expected, osmElement);
                    continue;
                }

                bool equal = comparison.CustomEqualityComparer != null
                    ? comparison.CustomEqualityComparer(actual, expected)
                    : string.Equals(actual, expected, StringComparison.Ordinal);

                if (!equal)
                    AddDifferent(report, subjectName, tag, actual, expected, osmElement);
            }
        }
    }

    
    private void AddMissing(Report report, string subjectName, string tag, string expected, OsmElement osmElement)
    {
        report.AddEntry(
            ReportGroup.TagSuggestions,
            new IssueReportEntry(
                "`" + subjectName + "` " + _subjectTypeLabel + " " +
                "is missing `" + tag + "=" + expected + "` - " +
                osmElement.OsmViewUrl,
                osmElement.AverageCoord,
                MapPointStyle.Problem,
                osmElement
            )
        );
    }

    private void AddDifferent(Report report, string subjectName, string tag, string actual, string expected, OsmElement osmElement)
    {
        report.AddEntry(
            ReportGroup.TagSuggestions,
            new IssueReportEntry(
                "`" + subjectName + "` " + _subjectTypeLabel + " " +
                "has `" + tag + "=" + actual + "` " +
                "but expecting `" + tag + "=" + expected + "` - " +
                osmElement.OsmViewUrl,
                osmElement.AverageCoord,
                MapPointStyle.Problem,
                osmElement
            )
        );
    }

    private enum ReportGroup
    {
        TagSuggestions = -9 // before most analyzer extra issues, after correlation
    }
}