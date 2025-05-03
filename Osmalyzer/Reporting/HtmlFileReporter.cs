using System.Diagnostics;
using System.Reflection;
using System.Web;

namespace Osmalyzer;

public class HtmlFileReporter : Reporter
{
    public HtmlFileReporter()
    {
        if (Directory.Exists(ReportWriter.OutputPath))
            Directory.Delete(ReportWriter.OutputPath, true);  
                
        Directory.CreateDirectory(ReportWriter.OutputPath);        
    }
    
    
    public override void Save()
    {
        string output = GetTemplate();

        string reportList = BuildReportListOutput();

        output = HtmlFileReportWriter.ReplaceLocatorBlock(output, "REPORTS", reportList);

        string timestamp = DateTime.UtcNow.ToString("R");
        
        output = HtmlFileReportWriter.ReplaceLocatorBlock(output, "TIMESTAMP", timestamp);

        
        File.WriteAllText(
            Path.Combine(ReportWriter.OutputPath, "index.html"),
            output
        );

        
        CopyIconsForLeaflet();
    }

    private string BuildReportListOutput()
    {
        StringBuilder stringBuilder = new StringBuilder();
        
        HtmlFileReportWriter reportWriter = new HtmlFileReportWriter();

        if (reports.Count > 0)
        {
            List<(AnalyzerGroup Key, List<Report>)> groupedReports = GetGroupedReports(reports);

            foreach ((AnalyzerGroup group, List<Report> reportsInGroup) in groupedReports)
            {
                stringBuilder.AppendLine("<h4>" + HttpUtility.HtmlEncode(group.Title) + "</h4>");

                stringBuilder.AppendLine("<ul>");

                foreach (Report report in reportsInGroup)
                {
                    Console.Write("Writing report for " + report.Name + "...");

                    Stopwatch saveStopwatch = Stopwatch.StartNew();

                    reportWriter.Save(report);

                    Console.WriteLine(" (" + saveStopwatch.ElapsedMilliseconds + " ms)");

                    stringBuilder.AppendLine("<li><a href=\"" + reportWriter.ReportFileName + "\">" + HttpUtility.HtmlEncode(report.Name) + "</a></li>");
                }

                stringBuilder.AppendLine("</ul>");
            }
        }
        else
        {
            stringBuilder.AppendLine("<p>No reports were generated.</p>");
        }

        if (skippedReports.Count > 0)
        {
            stringBuilder.AppendLine("<h4>Skipped</h4>");

            foreach ((string report, string reason) in skippedReports)
                stringBuilder.AppendLine("<li>" + HttpUtility.HtmlEncode(report) + " - " + HttpUtility.HtmlEncode(reason) + "</li>");
        }

        return stringBuilder.ToString();
    }


    private List<(AnalyzerGroup Key, List<Report>)> GetGroupedReports(IEnumerable<Report> ungroupedReports)
    {
        return ungroupedReports
               .GroupBy(r => r.Analyzer.Group)
               .Select(gr => (gr.Key, gr.OrderBy(g => g.Name).ToList()))
               .ToList();
    }

    private void CopyIconsForLeaflet()
    {
        foreach (EmbeddedIcon embeddedIcon in EmbeddedIcons.Icons)
            CopyIcon(embeddedIcon);

        return;
        
        
        static void CopyIcon(EmbeddedIcon embeddedIcon)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            const string resourcePrefix = @"Osmalyzer.Reporting.HTML_report_resources.";
            string resourcePath = resourcePrefix + embeddedIcon.Name;

            using Stream stream = assembly.GetManifestResourceStream(resourcePath)!;

            string outputPath = Path.Combine(ReportWriter.OutputPath, @"icons");
        
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);  
        
            FileStream fileStream = new FileStream(Path.Combine(outputPath, embeddedIcon.Name), FileMode.Create);
            StreamWriter streamWriter = new StreamWriter(fileStream);
            stream.CopyTo(streamWriter.BaseStream);
            streamWriter.Close();
            fileStream.Close();
        }
    }
    
    [Pure]
    private static string GetTemplate()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        
        const string resourcePath = @"Osmalyzer.Reporting.Report_templates.index.html";

        using Stream stream = assembly.GetManifestResourceStream(resourcePath)!;
        
        using StreamReader reader = new StreamReader(stream);
        
        return reader.ReadToEnd();
    }
}