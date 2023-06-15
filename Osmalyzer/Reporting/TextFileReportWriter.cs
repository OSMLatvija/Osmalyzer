using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Osmalyzer
{
    public class TextFileReportWriter : ReportWriter
    {
        private const string outputFolder = @"output";

        
        public override void Save(Report report, string name, List<AnalysisData> datas)
        {
            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);
            
            string reportFileName = outputFolder + "/" + name + @" report.txt";
            
            using StreamWriter reportFile = File.CreateText(reportFileName);

            reportFile.WriteLine("Report for " + name);
            reportFile.WriteLine();

            foreach (string line in report.lines)
                reportFile.WriteLine(line);
            
            reportFile.WriteLine();
            string dataDates = string.Join(", ", datas.Where(d => d.DataDate != null).Select(d => d.DataDate!.Value.ToString(CultureInfo.InvariantCulture)));
            reportFile.WriteLine("Data as of " + dataDates + ". Provided as is; mistakes possible.");
            
            reportFile.Close();
        }
    }
}