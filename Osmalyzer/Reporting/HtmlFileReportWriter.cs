using System.Collections.Generic;
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

            // Now comes the worst way to build an HTML page :)
            
            reportFile.WriteLine(@"<!doctype html>");
            reportFile.WriteLine(@"<html>");
            reportFile.WriteLine(@"<head>");
            reportFile.WriteLine(@"<title>" + HttpUtility.HtmlEncode(report.AnalyzerName) + " report</title>");
            reportFile.WriteLine(@"<meta name=""description"" content=""Osmalyzer " + report.AnalyzerName + @" report"" />");
            reportFile.WriteLine(@"<meta name=""keywords"" content=""html tutorial template"" />");
            reportFile.WriteLine(@"<meta http-equiv=""Cache-Control"" content=""no-cache, no-store, must-revalidate"" />");
            reportFile.WriteLine(@"<meta http-equiv=""Pragma"" content=""no-cache"" />");
            reportFile.WriteLine(@"<meta http-equiv=""Expires"" content=""0"" />");

            bool needMap = report.NeedMap;
            
            if (needMap)
            {
                 reportFile.WriteLine(@"<link rel=""stylesheet"" href=""https://unpkg.com/leaflet@1.9.4/dist/leaflet.css""");
                 reportFile.WriteLine(@"    integrity=""sha256-p4NxAoJBhIIN+hmNHrzRCf9tD/miZyoHS5obTRR9BMY=""");
                 reportFile.WriteLine(@"    crossorigin=""""/>");
                 
                 reportFile.WriteLine(@"<script src=""https://unpkg.com/leaflet@1.9.4/dist/leaflet.js""");
                 reportFile.WriteLine(@"    integrity=""sha256-20nQCchB9co0qIjJZRGuk2/Z9VM+kNiyxNV1lvTlZBo=""");
                 reportFile.WriteLine(@"    crossorigin=""""></script>");
            }
            
            reportFile.WriteLine(@"</head>");
            reportFile.WriteLine(@"<body>");
            
            reportFile.WriteLine("Report for " + HttpUtility.HtmlEncode(report.AnalyzerName) + "<br><br>");

            List<Report.ReportGroup> groups = report.CollectEntries();

            for (int g = 0; g < groups.Count; g++)
            {
                Report.ReportGroup group = groups[g];
                
                reportFile.WriteLine("<h3>" + group.Description + "</h3>");

                if (group.DescriptionEntry != null)
                    reportFile.WriteLine("<p>" + PolishLine(group.DescriptionEntry.Text) + "</p>");

                if (!group.HaveAnyContentEntries)
                    if (group.PlaceholderEntry != null)
                        reportFile.WriteLine("<p>" + PolishLine(group.PlaceholderEntry.Text) + "</p>");

                if (group.GenericEntries.Count > 0)
                {
                    foreach (Report.ReportEntry entry in group.GenericEntries)
                        reportFile.WriteLine("<p>" + PolishLine(entry.Text) + "</p>");
                }

                if (group.IssueEntries.Count > 0)
                {
                    reportFile.WriteLine("<ul>");
                    foreach (Report.ReportEntry entry in group.IssueEntries)
                        reportFile.WriteLine("<li>" + PolishLine(entry.Text) + "</li>");
                    reportFile.WriteLine("</ul>");
                }

                if (group.MapPointEntries.Count > 0)
                {
                    reportFile.WriteLine($@"<div id=""map{g}"" style=""width: 800px; height: 400px;""></div>");

                    reportFile.WriteLine(@"<script>");
                    reportFile.WriteLine(@$"var map = L.map('map{g}').setView([56.906, 24.505], 7);");
                    reportFile.WriteLine(@"L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {");
                    reportFile.WriteLine(@"    maxZoom: 21,");
                    reportFile.WriteLine(@"    attribution: '&copy; <a href=""https://www.openstreetmap.org/copyright"">OSM</a>'");
                    reportFile.WriteLine(@"}).addTo(map);");
                    foreach (Report.MapPointReportEntry mapPointEntry in group.MapPointEntries)
                    {
                        string lat = mapPointEntry.Lat.ToString("F6");
                        string lon = mapPointEntry.Lon.ToString("F6");
                        string text = HttpUtility.HtmlEncode(mapPointEntry.Text);
                        string? url = mapPointEntry.Url != null ? @$" <a href=\""{mapPointEntry.Url}\"" target=\""_blank\"">🔗</a>" : null;
                        reportFile.WriteLine($@"L.marker([{lat}, {lon}]).addTo(map).bindPopup(""{text}{url}"");");
                    }
                    reportFile.WriteLine(@"</script>");
                }
            }

            reportFile.WriteLine("<br>Data as of " + HttpUtility.HtmlEncode(report.AnalyzedDataDates) + ". Provided as is; mistakes possible.");
            
            reportFile.WriteLine(@"</body>");
            reportFile.WriteLine(@"</html>");
            
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
            
            line = Regex.Replace(line, @"(https://overpass-turbo.eu/\?Q=[a-zA-Z0-9%\-_\.!*()+]+)", @"<a href=""$1"">Query</a>");

            return line;
        }
    }
}