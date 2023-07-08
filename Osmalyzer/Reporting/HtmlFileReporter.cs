using System;
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
            
            reportFile.WriteLine(@"<!doctype html>");
            reportFile.WriteLine(@"<html>");
            reportFile.WriteLine(@"<head>");
            reportFile.WriteLine(@"<title>Osmalyzer reports</title>");
            reportFile.WriteLine(@"<meta name=""description"" content=""A list of Osmalyzer reports"" />");
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

            reportFile.WriteLine(@"</head>");
            reportFile.WriteLine(@"<body>");
            
            reportFile.WriteLine("<h3>Reports</h3>");

            HtmlFileReportWriter reportWriter = new HtmlFileReportWriter();
            
            reportFile.WriteLine("<ul>");

            foreach (Report report in reports)
            {
                reportWriter.Save(report);

                reportFile.WriteLine("<li><a href=\"" + reportWriter.ReportFileName + "\">" + HttpUtility.HtmlEncode(report.Name) + "</a></li>");
            }

            reportFile.WriteLine("</ul>");

            reportFile.WriteLine("<p>Reports generated " + DateTime.UtcNow.ToString("R") + ".</p>");

            
            reportFile.WriteLine("<h3>Disclaimer</h3>");

            reportFile.WriteLine("<p>Reports look for specific problems, but the very nature of OSM free tagging and public editing means there are endless possibilities. Thus no report is complete or exhaustive - there are false positives and false negatives.</p>");
            reportFile.WriteLine("<p>If you are fixing anything based on this, it's your responsibility to understand the actual issue and why and how it should be fixed. Not everything identified as an issue is an issue.</p>");
            reportFile.WriteLine("<p>This supposes that you are familiar with OSM and tagging. Reports can and do omit a lot of explanations.</p>");
            reportFile.WriteLine("<p>The sources used are almost always out of date. OSM may lag a day or more behind, while various other providers may be months or even years out of date.</p>");

            reportFile.WriteLine(@"</body>");
            reportFile.WriteLine(@"</html>");

            reportFile.Close();
        }
    }
}