using System.IO;

namespace Osmalyzer
{
    public class TextFileReporter : Reporter
    {
        public override void Save()
        {
            ReportWriter reportWriter = new TextFileReportWriter();
            
            if (Directory.Exists(ReportWriter.outputFolder))
                Directory.Delete(ReportWriter.outputFolder, true);  
                
            Directory.CreateDirectory(ReportWriter.outputFolder);
            
            foreach (Report report in reports)
                reportWriter.Save(report);
        }
    }
}