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

        public string AnalyzedDataDates { get; }

        
        public ReadOnlyCollection<string> Lines => _lines.AsReadOnly();


        private readonly List<string> _lines = new List<string>();


        public Report(Analyzer analyzer, IEnumerable<AnalysisData> datas)
        {
            AnalyzerName = analyzer.Name;
            AnalyzerDescription = analyzer.Description;
            AnalyzedDataDates = string.Join(", ", datas.Where(d => d.DataDate != null).Select(d => d.DataDate!.Value.ToString(CultureInfo.InvariantCulture)));
        }


        public void WriteLine(string line)
        {
            _lines.Add(line);
        }
    }
}