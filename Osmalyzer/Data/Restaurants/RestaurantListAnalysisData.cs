namespace Osmalyzer;

public abstract class RestaurantListAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public abstract IEnumerable<RestaurantData> Restaurant { get; }

    public override bool NeedsPreparation => true;
}