namespace Osmalyzer;

public abstract class Reporter
{
    protected readonly List<Report> reports = [ ];

    protected readonly List<(string report, string reason)> skippedReports = [ ];
        
        
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