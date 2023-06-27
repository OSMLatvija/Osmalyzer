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
                
                if (!group.HaveAnyContentEntries)
                    if (group.PlaceholderEntry != null)
                        reportFile.WriteLine(group.PlaceholderEntry.Text);
                
                if (group.GenericEntries.Count > 0)
                {
                    reportFile.WriteLine(group.Description);
                    foreach (Report.ReportEntry entry in group.GenericEntries)
                        reportFile.WriteLine("* " + entry.Text);
                    reportFile.WriteLine();
                }
                
                if (group.IssueEntries.Count > 0)
                {
                    reportFile.WriteLine(group.Description);
                    foreach (Report.ReportEntry entry in group.IssueEntries)
                        reportFile.WriteLine("* " + entry.Text);
                    reportFile.WriteLine();
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