using System.IO;

namespace Osmalyzer;

public class TextFileReporter : Reporter
{
    public TextFileReporter()
    {
        if (Directory.Exists(ReportWriter.OutputPath))
            Directory.Delete(ReportWriter.OutputPath, true);  
                
        Directory.CreateDirectory(ReportWriter.OutputPath);
    }
    
    
    public override void Save()
    {
        ReportWriter reportWriter = new TextFileReportWriter();

        foreach (Report report in reports)
            reportWriter.Save(report);
    }
}