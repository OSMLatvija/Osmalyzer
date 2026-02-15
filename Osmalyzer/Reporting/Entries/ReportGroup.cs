using System.Collections.ObjectModel;

namespace Osmalyzer;

public class ReportGroup
{
    /// <summary>
    /// Unique ID for this group, so that adding entries only needs to specify this id.
    /// This will also be used for sorting the groups if possible, so using an ordered enum would work well.  
    /// </summary>
    public object ID { get; }

    /// <summary>
    /// The ID of the "parent" group that should visually "contain" us.
    /// This doesn't organzie entries or anything else, this is just the user-facing organization.
    /// </summary>
    public object? ParentGroupId { get; }

    public string Title { get; }
    
    public bool ShowImportantEntryCount { get; }

    public bool ShouldClusterMapPointEntries { get; }


    public DescriptionReportEntry? DescriptionEntry { get; private set; }

    public PlaceholderReportEntry? PlaceholderEntry { get; private set; }
            
    public ReadOnlyCollection<MapPointReportEntry> MapPointEntries => _mapPointEntries.AsReadOnly();

        
    public int TotalEntryCount =>
        _genericEntries.Count + 
        _issuesEntries.Count + 
        _mapPointEntries.Count +
        (DescriptionEntry != null ? 1 : 0) +
        (PlaceholderEntry != null ? 1 : 0);

    public int ImportantEntryCount =>
        _genericEntries.Count +
        _issuesEntries.Count; 

    public int GenericEntryCount => _genericEntries.Count;

    public int IssueEntryCount => _issuesEntries.Count;
        
    /// <summary>
    /// This is basically for deciding to show <see cref="PlaceholderReportEntry"/>.
    /// </summary>
    public bool HaveAnyContentEntries => 
        _genericEntries.Count > 0 || 
        _issuesEntries.Count > 0 || 
        _mapPointEntries.Count > 0; // this counts too right? would there ever be only map points? report that is just map?

    public bool NeedMap => _mapPointEntries.Count > 0;


    private readonly List<GenericReportEntry> _genericEntries = [ ];
            
    private readonly List<IssueReportEntry> _issuesEntries = [ ];
    // todo: should I merge issue with generic? add some sort of "issue rating"?
            
    private readonly List<MapPointReportEntry> _mapPointEntries = [ ];


    public ReportGroup(object id, object? parentGroupId, string title, bool showImportantEntryCount, bool shouldClusterMapPointEntries)
    {
        if (id == null) throw new ArgumentNullException(nameof(id));
        if (id == parentGroupId) throw new ArgumentException("Group ID and parent group ID cannot be the same!", nameof(parentGroupId));
        
        ID = id;
        ParentGroupId = parentGroupId;
        Title = title;
        ShowImportantEntryCount = showImportantEntryCount;
        ShouldClusterMapPointEntries = shouldClusterMapPointEntries;
    }

            
    public void AddEntry(ReportEntry newEntry)
    {
        switch (newEntry)
        {
            case GenericReportEntry gre:
                _genericEntries.Add(gre);
                break;
                    
            case IssueReportEntry ire:
                _issuesEntries.Add(ire);
                break;
                    
            case MapPointReportEntry mpe:
                _mapPointEntries.Add(mpe);
                // Map Leaflet clustering will handle/spiderfy multiple nodes at the same location
                break;

            case PlaceholderReportEntry pe:
                if (PlaceholderEntry != null) throw new InvalidOperationException("Placeholder entry already set!");
                PlaceholderEntry = pe;
                break;
                    
            case DescriptionReportEntry de:
                if (DescriptionEntry != null) throw new InvalidOperationException("Description entry already set!");
                DescriptionEntry = de;
                break;
                    
            default:
                throw new ArgumentOutOfRangeException(nameof(newEntry));
        }
                
        if (newEntry.SubEntry != null)
            AddEntry(newEntry.SubEntry);
    }

    public void CancelEntries(ReportEntryContext context)
    {
        _issuesEntries.RemoveAll(e => e.Context == context);
        _genericEntries.RemoveAll(e => e.Context == context);
        _mapPointEntries.RemoveAll(e => e.Context == context);

        if (PlaceholderEntry != null && PlaceholderEntry.Context == context) PlaceholderEntry = null;
        if (DescriptionEntry != null && DescriptionEntry.Context == context) DescriptionEntry = null;
    }
        
    public ReadOnlyCollection<GenericReportEntry> CollectGenericEntries()
    {
        for (int i = 0; i < _genericEntries.Count; i++)
            _genericEntries[i].AdditionIndex = i;
        
        _genericEntries.Sort(new EntrySortingComparer());

        return _genericEntries.AsReadOnly();
    }
        
    public ReadOnlyCollection<IssueReportEntry> CollectIssueEntries()
    {
        for (int i = 0; i < _issuesEntries.Count; i++)
            _issuesEntries[i].AdditionIndex = i;
        
        _issuesEntries.Sort(new EntrySortingComparer());
            
        return _issuesEntries.AsReadOnly();
    }
}