using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Osmalyzer
{
    public class ReportGroup
    {
        public object ID { get; }
            
        public string Description { get; }


        public ReadOnlyCollection<ReportEntry> GenericEntries => _genericEntries.AsReadOnly();
            
        public ReadOnlyCollection<ReportEntry> IssueEntries => _issuesEntries.AsReadOnly();
            
        public ReadOnlyCollection<MapPointReportEntry> MapPointEntries => _mapPointEntries.AsReadOnly();

        public PlaceholderReportEntry? PlaceholderEntry { get; private set; }
            
        public DescriptionReportEntry? DescriptionEntry { get; private set; }

        public bool HaveAnyContentEntries => 
            _genericEntries.Count > 0 || 
            _issuesEntries.Count > 0 || 
            _mapPointEntries.Count > 0; // this counts too right? would there ever be only map points? report that is just map?

        public bool NeedMap => _mapPointEntries.Count > 0;


        private readonly List<ReportEntry> _genericEntries = new List<ReportEntry>();
            
        private readonly List<ReportEntry> _issuesEntries = new List<ReportEntry>();
            
        private readonly List<MapPointReportEntry> _mapPointEntries = new List<MapPointReportEntry>();


        public ReportGroup(object id, string description)
        {
            ID = id;
            Description = description;
        }

            
        public void AddEntry(ReportEntry newEntry)
        {
            switch (newEntry)
            {
                case GenericReportEntry:
                    _genericEntries.Add(newEntry);
                    break;
                    
                case IssueReportEntry:
                    _issuesEntries.Add(newEntry);
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

        public void RemoveEntry(ReportEntry entry)
        {
            _issuesEntries.Remove(entry);
        }
    }
}