namespace Osmalyzer;

public abstract class Reporter
{
    protected readonly List<Report> reports = new List<Report>();

    protected readonly List<(string report, string reason)> skippedReports = new List<(string report, string reason)>();
        
        
    public void AddReport(Report report)
    {
        reports.Add(report);
    }

    public void AddSkippedReport(string report, string reason)
    {
        skippedReports.Add((report, reason));
    }


    public abstract void Save();
}