using System.IO;
using System.Web;

namespace Osmalyzer
{
    public class HtmlFileReportWriter : ReportWriter
    {
        public string ReportFileName { get; private set; } = null!;


        public override void Save(Report report)
        {
            ReportFileName = report.AnalyzerName + @" report.html";
            
            string fullReportFileName = outputFolder + "/" + ReportFileName;
            
            using StreamWriter reportFile = File.CreateText(fullReportFileName);

            reportFile.WriteLine("Report for " + HttpUtility.HtmlEncode(report.AnalyzerName) + "<br><br>");

            foreach (string line in report.Lines)
                reportFile.WriteLine(line + "<br>");
            
            reportFile.WriteLine("<br>Data as of " + HttpUtility.HtmlEncode(report.AnalyzedDataDates) + ". Provided as is; mistakes possible.");
            
            reportFile.Close();
        }
    }
}