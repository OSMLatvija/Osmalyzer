using WikidataSharp;

namespace Osmalyzer;

/// <summary>
/// Base class for administrative analyzers with common external data matching issue reporting
/// </summary>
/// <typeparam name="T">The data item type for this analyzer</typeparam>
public abstract class AdminAnalyzerBase<T> : Analyzer
    where T : IDataItem, IHasWikidataItem, IHasVdbEntry
{
    protected void AddExternalDataMatchingIssuesGroup(
        Report report,
        object externalDataMatchingIssuesGroup
    )
    {
        report.AddGroup(
            externalDataMatchingIssuesGroup,
            "Extra data item matching issues",
            "This section lists any issues with data item matching to additional external data sources.",
            "No issues found."
        );
    }


    protected void ReportExtraAtvkEntries(
        Report report,
        object externalDataMatchingIssuesGroup,
        IReadOnlyList<AtvkEntry> atvkEntries,
        Dictionary<T, AtvkEntry> dataItemMatches,
        string itemTypeName
    )
    {
        List<AtvkEntry> extraAtvkEntries = atvkEntries
            .Where(e => !dataItemMatches.Values.Contains(e))
            .ToList();
        
        foreach (AtvkEntry atvkEntry in extraAtvkEntries)
        {
            report.AddEntry(
                externalDataMatchingIssuesGroup,
                new IssueReportEntry(
                    "ATVK entry for " + itemTypeName + " `" + atvkEntry.Name + "` (#`" + atvkEntry.Code + "`) was not matched to any OSM element."
                )
            );
        }
    }


    protected void ReportExtraWikidataItems(
        Report report,
        object externalDataMatchingIssuesGroup,
        IReadOnlyList<WikidataItem> wikidataItems,
        IReadOnlyList<T> dataItems,
        string itemTypeName
    )
    {
        List<WikidataItem> extraWikidataItems = wikidataItems
            .Where(wd => dataItems.All(c => c.WikidataItem != wd))
            .ToList();

        foreach (WikidataItem wikidataItem in extraWikidataItems)
        {
            string? name = wikidataItem.GetBestName("lv") ?? null;

            report.AddEntry(
                externalDataMatchingIssuesGroup,
                new IssueReportEntry(
                    "Wikidata " + itemTypeName + " item " + wikidataItem.WikidataUrl + (name != null ? " `" + name + "` " : "") + " was not matched to any OSM element."
                )
            );
        }
    }


    protected void ReportWikidataMatchIssues(
        Report report,
        object externalDataMatchingIssuesGroup,
        IReadOnlyList<WikidataData.WikidataMatchIssue> wikidataMatchIssues
    )
    {
        foreach (WikidataData.WikidataMatchIssue matchIssue in wikidataMatchIssues)
        {
            switch (matchIssue)
            {
                case WikidataData.MultipleWikidataMatchesWikidataMatchIssue<T> multipleWikidataMatches:
                    report.AddEntry(
                        externalDataMatchingIssuesGroup,
                        new IssueReportEntry(
                            multipleWikidataMatches.DataItem.ReportString() + " matched multiple Wikidata items: " +
                            string.Join(", ", multipleWikidataMatches.WikidataItems.Select(wd => wd.WikidataUrl))
                        )
                    );
                    break;
                
                case WikidataData.CoordinateMismatchWikidataMatchIssue<T> coordinateMismatch:
                    report.AddEntry(
                        externalDataMatchingIssuesGroup,
                        new IssueReportEntry(
                            coordinateMismatch.DataItem.ReportString() + " matched a Wikidata item, but the Wikidata coordinate is too far at " +
                            coordinateMismatch.DistanceMeters.ToString("F0") + " m" +
                            " -- " + coordinateMismatch.WikidataItem.WikidataUrl
                        )
                    );
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(matchIssue));
            }
        }
    }


    protected void ReportVdbMatchIssues(
        Report report,
        object externalDataMatchingIssuesGroup,
        IReadOnlyList<VdbMatchIssue> vdbMatchIssues
    )
    {
        foreach (VdbMatchIssue vdbMatchIssue in vdbMatchIssues)
        {
            switch (vdbMatchIssue)
            {
                case MultipleVdbMatchesVdbMatchIssue<T> multipleVdbMatches:
                    report.AddEntry(
                        externalDataMatchingIssuesGroup,
                        new IssueReportEntry(
                            multipleVdbMatches.DataItem.ReportString() + " matched multiple VDB entries: " +
                            string.Join(", ", multipleVdbMatches.VdbEntries.Select(vdb => vdb.ReportString()))
                        )
                    );
                    break;
                
                case CoordinateMismatchVdbMatchIssue<T> coordinateMismatch:
                    report.AddEntry(
                        externalDataMatchingIssuesGroup,
                        new IssueReportEntry(
                            coordinateMismatch.DataItem.ReportString() + " matched a VDB entry " + coordinateMismatch.VdbEntry.ReportString() + ", but the VDB coordinate is too far at " +
                            coordinateMismatch.DistanceMeters.ToString("F0") + " m"
                        )
                    );
                    break;
                
                case PoorMatchVdbMatchIssue<T> poorMatch:
                    report.AddEntry(
                        externalDataMatchingIssuesGroup,
                        new GenericReportEntry(
                            poorMatch.DataItem.ReportString() + " matched a VDB entry " + poorMatch.VdbEntry.ReportString() + ", but poorly as a fallback (and might be wrong)"
                        )
                    );
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(vdbMatchIssue));
            }
        }
    }


    protected void ReportMissingWikidataItems(
        Report report,
        object externalDataMatchingIssuesGroup,
        IReadOnlyList<T> dataItems
    )
    {
        foreach (T dataItem in dataItems)
        {
            if (dataItem.WikidataItem == null)
            {
                report.AddEntry(
                    externalDataMatchingIssuesGroup,
                    new IssueReportEntry(
                        dataItem.ReportString() + " does not have a matched Wikidata item."
                    )
                );
            }
        }
    }


    protected void ReportMissingVdbEntries(
        Report report,
        object externalDataMatchingIssuesGroup,
        IReadOnlyList<T> dataItems,
        IReadOnlyList<VdbEntry> vdbEntries
    )
    {
        foreach (T dataItem in dataItems)
        {
            if (dataItem.VdbEntry == null)
            {
                List<VdbEntry> potentials = vdbEntries.Where(e => e.Name == (dataItem.Name ?? "")).ToList();

                report.AddEntry(
                    externalDataMatchingIssuesGroup,
                    new IssueReportEntry(
                        dataItem.ReportString() + " does not have a matched VDB entry." +
                        (potentials.Count > 0 ? " Potential matches: " + string.Join(", ", potentials.Select(p => p.ReportString())) : "")
                    )
                );
            }
        }
    }


    protected void ReportUnmatchedOsmWikidataValues(
        Report report,
        object externalDataMatchingIssuesGroup,
        IReadOnlyList<T> dataItems,
        CorrelatorReport correlation
    )
    {
        foreach (MatchedCorrelation<T> match in correlation.Correlations.OfType<MatchedCorrelation<T>>())
        {
            if (match.DataItem.WikidataItem == null)
            {
                string? wikidata = match.OsmElement.GetValue("wikidata");

                if (wikidata != null && Regex.IsMatch(wikidata, @"^Q\d+$"))
                {
                    List<T> others = dataItems.Where(v => v.WikidataItem != null && v.WikidataItem!.QID == wikidata).ToList();

                    report.AddEntry(
                        externalDataMatchingIssuesGroup,
                        new IssueReportEntry(
                            match.DataItem.ReportString() + " has a `wikidata=" + wikidata + "` http://www.wikidata.org/entity/" + wikidata + " on OSM element " + match.OsmElement.OsmViewUrl +
                            " but the matched data item did not match to a Wikidata element." +
                            (others.Count > 0 ? " This Wikidata item was matched to other entries: " + string.Join(", ", others.Select(v => v.ReportString())) : "")
                        )
                    );
                }
            }
        }
    }
}
