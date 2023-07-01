using System;
using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer
{
    public class Report
    {
        public string AnalyzerName { get; }
        
        public string? AnalyzerDescription { get; }

        public string? AnalyzedDataDates { get; }

        
        public bool NeedMap => _groups.Any(g => g.NeedMap);


        private readonly List<ReportGroup> _groups = new List<ReportGroup>();


        public Report(Analyzer analyzer, IEnumerable<AnalysisData> datas)
        {
            AnalyzerName = analyzer.Name;
            
            AnalyzerDescription = analyzer.Description;

            List<ICachableAnalysisData> datasWithDate = datas.OfType<ICachableAnalysisData>().ToList();

            if (datasWithDate.Count > 0)
                AnalyzedDataDates = string.Join(", ", datasWithDate.Select(d => (d.DataDateHasDayGranularity ? ((AnalysisData)d).DataDate!.Value.ToString("yyyy-MM-dd HH:mm:ss") : ((AnalysisData)d).DataDate!.Value.ToString("yyyy-MM-dd")) + (datasWithDate.Count > 1 ? " (" + ((AnalysisData)d).Name + ")" : "")));
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

        public List<ReportGroup> CollectGroups()
        {
            return _groups.ToList();
        }

        public void CancelEntries(object groupId, ReportEntryContext context)
        {
            if (_groups.All(g => !Equals(g.ID, groupId))) throw new InvalidOperationException("Group \"" + groupId + "\" has not been created!");

            
            ReportGroup group = _groups.First(g => Equals(g.ID, groupId));

            group.CancelEntries(context);
        }
    }
}