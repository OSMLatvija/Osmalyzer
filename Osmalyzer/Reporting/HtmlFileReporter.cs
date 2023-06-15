using System.IO;
using System.Web;

namespace Osmalyzer
{
    public class HtmlFileReporter : Reporter
    {
        public override void Save()
        {
            if (Directory.Exists(ReportWriter.outputFolder))
                Directory.Delete(ReportWriter.outputFolder, true);  
                
            Directory.CreateDirectory(ReportWriter.outputFolder);

            using StreamWriter reportFile = File.CreateText(ReportWriter.outputFolder + "/index.html");

            reportFile.WriteLine("Reports:");

            HtmlFileReportWriter reportWriter = new HtmlFileReportWriter();
            
            reportFile.WriteLine("<ul>");

            foreach (Report report in reports)
            {
                reportWriter.Save(report);

                reportFile.WriteLine("<li><a href=\"" + reportWriter.ReportFileName + "\">" + HttpUtility.HtmlEncode(report.AnalyzerName) + "</a>" + (report.AnalyzerDescription != null ? " - " + HttpUtility.HtmlEncode(report.AnalyzerDescription) : "") + "</li>");
            }

            reportFile.WriteLine("</ul>");

            reportFile.Close();
        }
    }
}