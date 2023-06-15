using System.IO;

namespace Osmalyzer
{
    public class TextFileReporter : Reporter
    {
        public override void Save()
        {
            if (Directory.Exists(ReportWriter.outputFolder))
                Directory.Delete(ReportWriter.outputFolder, true);  
                
            Directory.CreateDirectory(ReportWriter.outputFolder);

            ReportWriter reportWriter = new TextFileReportWriter();

            foreach (Report report in reports)
                reportWriter.Save(report);
        }
    }
}