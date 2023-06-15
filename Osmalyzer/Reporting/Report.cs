using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace Osmalyzer
{
    public class Report
    {
        public string AnalyzerName { get; }
        
        public string? AnalyzerDescription { get; }

        public string? AnalyzedDataDates { get; }

        
        public ReadOnlyCollection<string> Lines => _lines.AsReadOnly();


        private readonly List<string> _lines = new List<string>();


        public Report(Analyzer analyzer, IEnumerable<AnalysisData> datas)
        {
            AnalyzerName = analyzer.Name;
            
            AnalyzerDescription = analyzer.Description;

            List<AnalysisData> datasWithDate = datas.Where(d => d.DataDate != null).ToList();

            if (datasWithDate.Count > 0)
                AnalyzedDataDates = string.Join(", ", datasWithDate.Select(d => (d.DataDateHasDayGranularity!.Value ? d.DataDate!.Value.ToString("yyyy-MM-dd HH:mm:ss") : d.DataDate!.Value.ToString("yyyy-MM-dd")) + (datasWithDate.Count > 1 ? " (" + d.Name + ")" : "")));
        }


        public void WriteLine(string line)
        {
            _lines.Add(line);
        }
    }
}