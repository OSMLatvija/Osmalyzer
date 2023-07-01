using System.Collections.Generic;
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

            // Now comes the worst way to build an HTML page :)
            
            reportFile.WriteLine(@"<!doctype html>");
            reportFile.WriteLine(@"<html>");
            reportFile.WriteLine(@"<head>");
            reportFile.WriteLine(@"<title>" + HttpUtility.HtmlEncode(report.AnalyzerName) + " report</title>");
            reportFile.WriteLine(@"<meta name=""description"" content=""Osmalyzer " + report.AnalyzerName + @" report"" />");
            reportFile.WriteLine(@"<meta http-equiv=""Cache-Control"" content=""no-cache, no-store, must-revalidate"" />");
            reportFile.WriteLine(@"<meta http-equiv=""Pragma"" content=""no-cache"" />");
            reportFile.WriteLine(@"<meta http-equiv=""Expires"" content=""0"" />");
            
            reportFile.WriteLine(@"<style>"); // todo: stylesheets? never heard of it
            reportFile.WriteLine(@"body {");
            reportFile.WriteLine(@"  font-family: Arial, sans-serif;");
            reportFile.WriteLine(@"  margin: 0;");
            reportFile.WriteLine(@"  padding: 20px;");
            reportFile.WriteLine(@"  background-color: #f2f2f2;");
            reportFile.WriteLine(@"  color: #333;");
            reportFile.WriteLine(@"}");
            reportFile.WriteLine(@"h1, h2, h3 {");
            reportFile.WriteLine(@"  color: #555;");
            reportFile.WriteLine(@"}");
            reportFile.WriteLine(@"a {");
            reportFile.WriteLine(@"  color: #007bff;");
            reportFile.WriteLine(@"  text-decoration: none;");
            reportFile.WriteLine(@"}");
            reportFile.WriteLine(@"a:hover {");
            reportFile.WriteLine(@"  text-decoration: underline;");
            reportFile.WriteLine(@"}");
            reportFile.WriteLine(@"a:visited {");
            reportFile.WriteLine(@"  color: #1b4b99;");
            reportFile.WriteLine(@"}");
            reportFile.WriteLine(@"</style>");

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

            List<ReportGroup> groups = report.CollectGroups();

            for (int g = 0; g < groups.Count; g++)
            {
                ReportGroup group = groups[g];
                
                reportFile.WriteLine("<h3>" + group.Description + "</h3>");

                if (group.DescriptionEntry != null)
                    reportFile.WriteLine("<p>" + PolishLine(group.DescriptionEntry.Text) + "</p>");

                if (!group.HaveAnyContentEntries)
                    if (group.PlaceholderEntry != null)
                        reportFile.WriteLine("<p>" + PolishLine(group.PlaceholderEntry.Text) + "</p>");

                if (group.GenericEntryCount > 0)
                {
                    foreach (GenericReportEntry entry in group.CollectGenericEntries())
                        reportFile.WriteLine("<p>" + PolishLine(entry.Text) + "</p>");
                }

                if (group.IssueEntryCount > 0)
                {
                    reportFile.WriteLine("<ul>");
                    foreach (IssueReportEntry entry in group.CollectIssueEntries())
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
                    foreach (MapPointReportEntry mapPointEntry in group.MapPointEntries)
                    {
                        string lat = mapPointEntry.Coord.lat.ToString("F6");
                        string lon = mapPointEntry.Coord.lon.ToString("F6");
                        string text = PolishLine(mapPointEntry.Text).Replace("\"", "\\\"");
                        string mapUrl = @"<a href=\""" + mapPointEntry.Coord.OsmUrl + @"\"" target=\""_blank\"" title=\""Open map at this location\"">🔗</a>";
                        reportFile.WriteLine($@"L.marker([{lat}, {lon}]).addTo(map).bindPopup(""{text} {mapUrl}"");");
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