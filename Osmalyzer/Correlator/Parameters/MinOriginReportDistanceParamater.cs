namespace Osmalyzer;

public class MinOriginReportDistanceParamater : CorrelatorParamater
{
    public double MinDistance { get; }

    
    public MinOriginReportDistanceParamater(double minDistance)
    {
        MinDistance = minDistance;
    }
}