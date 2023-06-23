﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Osmalyzer
{
    public class Report
    {
        public string AnalyzerName { get; }
        
        public string? AnalyzerDescription { get; }

        public string? AnalyzedDataDates { get; }

        
        public ReadOnlyCollection<string> RawLines => _rawLines.AsReadOnly();
        // TODO: remove this


        private readonly List<string> _rawLines = new List<string>();
        
        private readonly List<ReportGroup> _groups = new List<ReportGroup>();


        public Report(Analyzer analyzer, IEnumerable<AnalysisData> datas)
        {
            AnalyzerName = analyzer.Name;
            
            AnalyzerDescription = analyzer.Description;

            List<AnalysisData> datasWithDate = datas.Where(d => d.DataDate != null).ToList();

            if (datasWithDate.Count > 0)
                AnalyzedDataDates = string.Join(", ", datasWithDate.Select(d => (d.DataDateHasDayGranularity!.Value ? d.DataDate!.Value.ToString("yyyy-MM-dd HH:mm:ss") : d.DataDate!.Value.ToString("yyyy-MM-dd")) + (datasWithDate.Count > 1 ? " (" + d.Name + ")" : "")));
        }


        public void WriteRawLine(string line)
        {
            _rawLines.Add(line);
        }

        public void AddGroup(object id, string description)
        {
            _groups.Add(new ReportGroup(id, description));
        }

        public void AddEntry(object groupId, ReportEntry newEntry)
        {
            if (_groups.All(g => !Equals(g.ID, groupId))) throw new InvalidOperationException("Group \"" + groupId + "\" has not been created!");
            
            
            ReportGroup group = _groups.First(g => Equals(g.ID, groupId));

            group.AddEntry(newEntry);
        }

        public List<ReportGroup> CollectEntries()
        {
            // TODO: organize
            
            return _groups.ToList();
        }

        public void CancelEntry(object groupId, object context)
        {
            if (_groups.All(g => !Equals(g.ID, groupId))) throw new InvalidOperationException("Group \"" + groupId + "\" has not been created!");

            
            ReportGroup group = _groups.First(g => Equals(g.ID, groupId));

            ReportEntry entry = group.MainEntries.First(e => e.Context == context);

            group.RemoveEntry(entry);
        }


        public class ReportGroup
        {
            public object ID { get; }
            
            public string Description { get; }


            public ReadOnlyCollection<ReportEntry> MainEntries => _mainEntries.AsReadOnly();

            public ReportEntry? PlaceholderEntry { get; private set; }


            private readonly List<ReportEntry> _mainEntries = new List<ReportEntry>();


            public ReportGroup(object id, string description)
            {
                ID = id;
                Description = description;
            }

            
            public void AddEntry(ReportEntry newEntry)
            {
                switch (newEntry)
                {
                    case MainReportEntry:
                        _mainEntries.Add(newEntry);
                        break;
                    
                    case PlaceholderReportEntry:
                        if (PlaceholderEntry != null) throw new InvalidOperationException("Placeholder entry already set!");
                        PlaceholderEntry = newEntry;
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException(nameof(newEntry));
                }
            }

            public void RemoveEntry(ReportEntry entry)
            {
                _mainEntries.Remove(entry);
            }
        }

        public abstract class ReportEntry
        {
            public string Text { get; }
            
            public object? Context { get; }


            protected ReportEntry(string text, object? context = null)
            {
                Text = text;
                Context = context;
            }
        }

        public class MainReportEntry : ReportEntry
        {
            public MainReportEntry(string text, object? context = null)
                : base(text, context)
            {
            }
        }

        /// <summary> Shown if there are no other entries to show </summary>
        public class PlaceholderReportEntry : ReportEntry
        {
            public PlaceholderReportEntry(string text)
                : base(text)
            {
            }
        }
    }
}