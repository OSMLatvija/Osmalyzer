using System.IO;

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

            foreach (Report.ReportGroup group in report.CollectEntries())
            {
                reportFile.WriteLine(group.Description);
                foreach (Report.ReportEntry entry in group.Entries)
                    reportFile.WriteLine(entry.Text);
                reportFile.WriteLine();
            }

            foreach (string line in report.RawLines)
                reportFile.WriteLine(line);
            
            reportFile.WriteLine();
            reportFile.WriteLine((report.AnalyzedDataDates != null ? "Data as of " + report.AnalyzedDataDates + ". " : "") + "Provided as is; mistakes possible.");
            
            reportFile.Close();
        }
    }
}