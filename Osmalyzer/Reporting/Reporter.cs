using System.Collections.Generic;

namespace Osmalyzer;

public abstract class Reporter
{
    protected readonly List<Report> reports = new List<Report>();
        
        
    public void AddReport(Report report)
    {
        reports.Add(report);
    }

        
    public abstract void Save();
}