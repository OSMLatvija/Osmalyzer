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

            foreach (ReportGroup group in report.CollectGroups())
            {
                reportFile.WriteLine(group.Description);
                reportFile.WriteLine();

                if (group.DescriptionEntry != null)
                    reportFile.WriteLine(group.DescriptionEntry);
                
                if (!group.HaveAnyContentEntries)
                    if (group.PlaceholderEntry != null)
                        reportFile.WriteLine(group.PlaceholderEntry.Text);
                
                if (group.GenericEntryCount > 0)
                {
                    foreach (GenericReportEntry entry in group.CollectGenericEntries())
                        reportFile.WriteLine("* " + entry.Text);
                    reportFile.WriteLine();
                }
                
                if (group.IssueEntryCount > 0)
                {
                    foreach (IssueReportEntry entry in group.CollectIssueEntries())
                        reportFile.WriteLine("* " + entry.Text);
                    reportFile.WriteLine();
                }
                
                if (group.MapPointEntries.Count > 0)
                {
                    foreach (MapPointReportEntry entry in group.MapPointEntries)
                        reportFile.WriteLine("* " + entry.Coord + ": " + entry.Text);
                    reportFile.WriteLine();
                }
                
                reportFile.WriteLine();
            }
            
            reportFile.WriteLine();
            reportFile.WriteLine((report.AnalyzedDataDates != null ? "Data as of " + report.AnalyzedDataDates + ". " : "") + "Provided as is; mistakes possible.");
            
            reportFile.Close();
        }
    }
}