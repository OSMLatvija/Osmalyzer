namespace Osmalyzer;

public abstract class BankPointAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public List<BankPoint> Points { get; protected set; } = null!; // only null before prepared
}