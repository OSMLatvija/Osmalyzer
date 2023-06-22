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


        private readonly List<string> _rawLines = new List<string>();
        
        private readonly List<ReportEntry> _entries = new List<ReportEntry>();


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

        public void WriteEntry(string group, string text)
        {
            _entries.Add(new ReportEntry(group, text));
        }

        public List<ReportEntry> CollectEntries()
        {
            return _entries.ToList();
        }


        public class ReportEntry
        {
            public string Group { get; }
            
            public string Text { get; }
            

            public ReportEntry(string group, string text)
            {
                Group = group;
                Text = text;
            }
        }
    }
}