using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Osmalyzer
{
    public class ReportGroup
    {
        public object ID { get; }
            
        public string Title { get; }


        public DescriptionReportEntry? DescriptionEntry { get; private set; }

        public PlaceholderReportEntry? PlaceholderEntry { get; private set; }
            
        public ReadOnlyCollection<MapPointReportEntry> MapPointEntries => _mapPointEntries.AsReadOnly();

        
        public int TotalEntryCount =>
            _genericEntries.Count + 
            _issuesEntries.Count + 
            _mapPointEntries.Count +
            (DescriptionEntry != null ? 1 : 0) +
            (PlaceholderEntry != null ? 1 : 0);

        public int GenericEntryCount => _genericEntries.Count;

        public int IssueEntryCount => _issuesEntries.Count;
        
        public bool HaveAnyContentEntries => 
            _genericEntries.Count > 0 || 
            _issuesEntries.Count > 0 || 
            _mapPointEntries.Count > 0; // this counts too right? would there ever be only map points? report that is just map?

        public bool NeedMap => _mapPointEntries.Count > 0;


        private readonly List<GenericReportEntry> _genericEntries = new List<GenericReportEntry>();
            
        private readonly List<IssueReportEntry> _issuesEntries = new List<IssueReportEntry>();
        // todo: should I merge issue with generic? add some sort of "issue rating"?
            
        private readonly List<MapPointReportEntry> _mapPointEntries = new List<MapPointReportEntry>();


        public ReportGroup(object id, string title)
        {
            ID = id;
            Title = title;
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
            _genericEntries.Sort(new EntrySortingComparer());

            return _genericEntries.AsReadOnly();
        }
        
        public ReadOnlyCollection<IssueReportEntry> CollectIssueEntries()
        {
            _issuesEntries.Sort(new EntrySortingComparer());
            
            return _issuesEntries.AsReadOnly();
        }
    }
}