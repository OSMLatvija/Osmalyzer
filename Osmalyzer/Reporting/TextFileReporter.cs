using System.IO;

namespace Osmalyzer;

public class TextFileReporter : Reporter
{
    public override void Save()
    {
        if (Directory.Exists(ReportWriter.OutputPath))
            Directory.Delete(ReportWriter.OutputPath, true);  
                
        Directory.CreateDirectory(ReportWriter.OutputPath);

        ReportWriter reportWriter = new TextFileReportWriter();

        foreach (Report report in reports)
            reportWriter.Save(report);
    }
}