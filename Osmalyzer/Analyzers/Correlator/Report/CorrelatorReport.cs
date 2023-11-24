using System.Collections.Generic;

namespace Osmalyzer;

public class CorrelatorReport
{
    public List<Correlation> Correlations { get; }

        
    public CorrelatorReport(List<Correlation> correlations)
    {
        Correlations = correlations;
    }
}