namespace Osmalyzer;

public abstract class ShopListAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public abstract IEnumerable<ShopData> Shops { get; }

    public override bool NeedsPreparation => true;
}