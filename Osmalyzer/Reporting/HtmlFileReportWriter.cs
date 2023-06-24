using System.IO;
using System.Linq;
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

            foreach (Report.ReportGroup group in report.CollectEntries())
            {
                reportFile.WriteLine("<h3>" + group.Description + "</h3>");
                
                if (group.DescriptionEntry != null)
                    reportFile.WriteLine("<p>" + PolishLine(group.DescriptionEntry.Text) + "</p>");

                if (group.MainEntries.Count == 0)
                {
                    if (group.PlaceholderEntry != null)
                        reportFile.WriteLine("<p>" + PolishLine(group.PlaceholderEntry.Text) + "</p>");
                    else if (group.DescriptionEntry == null)
                        reportFile.WriteLine("<p>🕸</p>");
                }
                else if (group.MainEntries.Count > 1)
                {
                    reportFile.WriteLine("<ul>");
                    foreach (Report.ReportEntry entry in group.MainEntries)
                        reportFile.WriteLine("<li>" + PolishLine(entry.Text) + "</li>");
                    reportFile.WriteLine("</ul>");
                }
                else
                {
                    reportFile.WriteLine("<p>" + PolishLine(group.MainEntries.First().Text) + "</p>");
                }
            }

            if (report.RawLines.Any())
            {
                reportFile.WriteLine("<h3>Other issues and/or notes</h3>");
                
                reportFile.WriteLine("<ul>");
                foreach (string line in report.RawLines)
                    reportFile.WriteLine("<li>" + PolishLine(line) + "</li>");
                reportFile.WriteLine("</ul>");
            }

            reportFile.WriteLine("<br>Data as of " + HttpUtility.HtmlEncode(report.AnalyzedDataDates) + ". Provided as is; mistakes possible.");
            
            reportFile.Close();
        }

        
        private string PolishLine(string line)
        {
            line = HttpUtility.HtmlEncode(line);
            
            line = Regex.Replace(line, @"(https://www.openstreetmap.org/node/(\d+))", @"<a href=""$1"">Node #$2</a>");
            line = Regex.Replace(line, @"(https://www.openstreetmap.org/way/(\d+))", @"<a href=""$1"">Way #$2</a>");
            line = Regex.Replace(line, @"(https://www.openstreetmap.org/relation/(\d+))", @"<a href=""$1"">Relation #$2</a>");
            line = Regex.Replace(line, @"(https://www.openstreetmap.org/changeset/(\d+))", @"<a href=""$1"">Changeset #$2</a>");
            line = Regex.Replace(line, @"(https://www.openstreetmap.org/#map=\d{1,2}/(-?\d{1,3}\.\d+)/(-?\d{1,3}\.\d+))", @"<a href=""$1"">Location $2, $3</a>");

            return line;
        }
    }
}