namespace Osmalyzer;

public class CorrelatorReport
{
    public List<Correlation> Correlations { get; }

        
    public CorrelatorReport(List<Correlation> correlations)
    {
        Correlations = correlations;
    }

    public CorrelatorReport(params CorrelatorReport[] report)
    {
        Correlations = report.SelectMany(r => r.Correlations).ToList();
    }
}