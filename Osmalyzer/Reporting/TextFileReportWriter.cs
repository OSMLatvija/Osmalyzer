using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Osmalyzer
{
    public class TextFileReportWriter : ReportWriter
    {
        public override void Save(Report report)
        {
            string reportFileName = outputFolder + "/" + report.AnalyzerName + @" report.txt";
            
            using StreamWriter reportFile = File.CreateText(reportFileName);

            reportFile.WriteLine("Report for " + report.AnalyzerName);
            reportFile.WriteLine();

            foreach (string line in report.Lines)
                reportFile.WriteLine(line);
            
            reportFile.WriteLine();
            reportFile.WriteLine("Data as of " + report.AnalyzedDataDates + ". Provided as is; mistakes possible.");
            
            reportFile.Close();
        }
    }
}