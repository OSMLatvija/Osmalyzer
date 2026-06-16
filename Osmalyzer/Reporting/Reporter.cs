namespace Osmalyzer;

public abstract class Reporter
{
    protected readonly List<Report> reports = [ ];

    protected readonly List<(string report, string reason)> skippedReports = [ ];

    protected Dictionary<AnalysisData, List<string>> packagedDataFilesMapping = new Dictionary<AnalysisData, List<string>>();
        
        
    public void AddReport(Report report)
    {
        reports.Add(report);
    }

    public void AddSkippedReport(string report, string reason)
    {
        skippedReports.Add((report, reason));
    }

    public void SetPackagedDataFiles(Dictionary<AnalysisData, List<string>> mapping)
    {
        packagedDataFilesMapping = mapping;
    }


    public abstract void Save();
}