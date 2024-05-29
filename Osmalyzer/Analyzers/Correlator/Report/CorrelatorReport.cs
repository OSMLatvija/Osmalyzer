using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer;

public class CorrelatorReport
{
    public List<Correlation> Correlations { get; }

        
    public CorrelatorReport(List<Correlation> correlations)
    {
        Correlations = correlations;
    }

    public CorrelatorReport(params CorrelatorReport[] kioskReport)
    {
        Correlations = kioskReport.SelectMany(report => report.Correlations).ToList();
    }
}