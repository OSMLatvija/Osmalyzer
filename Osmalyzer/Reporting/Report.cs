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


    public void AddGroup(object id, string title, string? descriptionEntry = null, string? placeholderEntry = null, bool showImportantEntryCount = true)
    {
        ReportGroup newGroup = new ReportGroup(id, title, showImportantEntryCount);
            
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

    public List<ReportGroup> CollectGroups()
    {
        return _groups.OrderBy(g => GetSortOrder(g.ID)).ToList();

            
        static int GetSortOrder(object value)
        {
            if (value is int pureValue)
                return pureValue;
                
            if (value.GetType().IsEnum)
                return (int)value;

            return 0; // stays in the "middle"
        }
    }

    public void CancelEntries(object groupId, ReportEntryContext context)
    {
        if (_groups.All(g => !Equals(g.ID, groupId))) throw new InvalidOperationException("Group \"" + groupId + "\" has not been created!");

            
        ReportGroup group = _groups.First(g => Equals(g.ID, groupId));

        group.CancelEntries(context);
    }
}