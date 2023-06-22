using System.IO;
using System.Text.RegularExpressions;
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

            foreach (Report.ReportEntry entry in report.CollectEntries())
                reportFile.WriteLine(PolishLine(entry.Text) + "<br>");

            foreach (string line in report.RawLines)
                reportFile.WriteLine(PolishLine(line) + "<br>");
            
            reportFile.WriteLine("<br>Data as of " + HttpUtility.HtmlEncode(report.AnalyzedDataDates) + ". Provided as is; mistakes possible.");
            
            reportFile.Close();
        }

        
        private string PolishLine(string line)
        {
            line = Regex.Replace(line, @"(https://www.openstreetmap.org/node/(\d+))", @"<a href=""$1"">Node #$2</a>");
            line = Regex.Replace(line, @"(https://www.openstreetmap.org/way/(\d+))", @"<a href=""$1"">Way #$2</a>");
            line = Regex.Replace(line, @"(https://www.openstreetmap.org/relation/(\d+))", @"<a href=""$1"">Relation #$2</a>");
            line = Regex.Replace(line, @"(https://www.openstreetmap.org/changeset/(\d+))", @"<a href=""$1"">Changeset #$2</a>");
            line = Regex.Replace(line, @"(https://www.openstreetmap.org/#map=\d{1,2}/(-?\d{1,3}\.\d+)/(-?\d{1,3}\.\d+))", @"<a href=""$1"">Location $2, $3</a>");

            return line;
        }
    }
}