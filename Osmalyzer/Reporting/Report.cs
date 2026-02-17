namespace Osmalyzer;

public class Report
{
    public Analyzer Analyzer { get; }

    public string Name { get; }
        
    public string Description { get; }

    public IEnumerable<AnalysisData> Datas { get; }

        
    public bool NeedMap => _groups.Any(g => g.NeedMap);


    private readonly List<ReportGroup> _groups = new List<ReportGroup>();


    public Report(Analyzer analyzer, IEnumerable<AnalysisData> datas)
    {
        Analyzer = analyzer;
        Datas = datas;

        Name = analyzer.Name;
        Description = analyzer.Description;
    }


    public void AddGroup(object id, string title, string? descriptionEntry = null, string? placeholderEntry = null, bool showImportantEntryCount = true, bool shouldClusterMapPointEntries = true)
    {
        AddGroup(id, null, title, descriptionEntry, placeholderEntry, showImportantEntryCount, shouldClusterMapPointEntries);
    }
    
    public void AddGroup(object id, object? parentGroupId, string title, string? descriptionEntry = null, string? placeholderEntry = null, bool showImportantEntryCount = true, bool shouldClusterMapPointEntries = true)
    {
        //if (_groups.Any(g => g.Title == title && g.ParentGroupId == parentGroupId)) throw new InvalidOperationException("Group with title \"" + title + "\" (for parent group \"" + parentGroupId + "\") already exists!");
        
        ReportGroup newGroup = new ReportGroup(id, parentGroupId, title, showImportantEntryCount, shouldClusterMapPointEntries);
            
        _groups.Add(newGroup);
            
        if (descriptionEntry != null)
            newGroup.AddEntry(new DescriptionReportEntry(descriptionEntry));
            
        if (placeholderEntry != null)
            newGroup.AddEntry(new PlaceholderReportEntry(placeholderEntry));
    }

    public void AddEntry(object groupId, ReportEntry newEntry)
    {
        if (_groups.All(g => !Equals(g.ID, groupId))) throw new InvalidOperationException("Group \"" + groupId + "\" has not been created!");
            
            
        ReportGroup group = _groups.First(g => Equals(g.ID, groupId));

        group.AddEntry(newEntry);
    }

    [Pure]
    public List<ReportGroup> CollectGroups()
    {
        List<ReportGroup> parentGroups =
            _groups
                .Where(g => g.ParentGroupId != null)
                .Select(g => g.ParentGroupId)
                .Distinct()
                .Select(pgid => _groups.FirstOrDefault(g => g.ID == pgid))
                .Where(pg => pg != null)
                .ToList()!;

        List<ReportGroup> unparentedGroups =
            _groups
                .Where(g => g.ParentGroupId == null || parentGroups.All(pg => pg.ID != g.ParentGroupId))
                .ToList();

        List<ReportGroup> topGroups = parentGroups.Concat(unparentedGroups).OrderBy(GetSortOrder).ToList();
        
        List<ReportGroup> groups = [ ];

        foreach (ReportGroup topGroup in topGroups)
        {
            groups.Add(topGroup);

            groups.AddRange(
                _groups
                    .Where(g => g.ParentGroupId == topGroup.ID)
                    .OrderBy(GetSortOrder)
                    .ToList()
            );
        }

        return groups;


        [Pure]
        static int GetSortOrder(ReportGroup reportGroup)
        {
            return GetGroupIDValue(reportGroup);


            [Pure]
            static int GetGroupIDValue(ReportGroup reportGroup)
            {
                if (reportGroup.ID is int pureValue)
                    return pureValue;

                if (reportGroup.ID.GetType().IsEnum)
                    return (int)reportGroup.ID;

                return 0; // stays in the "middle" or doesn't get moved
            }
        }
    }

    public void CancelEntries(object groupId, ReportEntryContext context)
    {
        if (_groups.All(g => !Equals(g.ID, groupId))) throw new InvalidOperationException("Group \"" + groupId + "\" has not been created!");

            
        ReportGroup group = _groups.First(g => Equals(g.ID, groupId));

        group.CancelEntries(context);
    }
}