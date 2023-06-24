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
                if (group.DescriptionEntry != null)
                    reportFile.WriteLine(group.DescriptionEntry);
                
                if (group.MainEntries.Count > 0)
                {
                    reportFile.WriteLine(group.Description);
                    foreach (Report.ReportEntry entry in group.MainEntries)
                        reportFile.WriteLine(entry.Text);
                    reportFile.WriteLine();
                }
                else
                {
                    if (group.PlaceholderEntry != null)
                        reportFile.WriteLine(group.PlaceholderEntry);
                }
            }

            foreach (string line in report.RawLines)
                reportFile.WriteLine(line);
            
            reportFile.WriteLine();
            reportFile.WriteLine((report.AnalyzedDataDates != null ? "Data as of " + report.AnalyzedDataDates + ". " : "") + "Provided as is; mistakes possible.");
            
            reportFile.Close();
        }
    }
}