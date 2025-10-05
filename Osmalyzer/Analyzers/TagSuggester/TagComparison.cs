namespace Osmalyzer;

/// <summary>
/// Used by <see cref="TagSuggester{TDataItem}"/> to define what tag to compare
/// between <see cref="OsmElement"/> and <typeparamref name="TDataItem"/>.
/// </summary>
public class TagComparison<TDataItem> where TDataItem : IDataItem
{
    public string OsmKey { get; }
    
    public Func<TDataItem, string?> ExpectedValueSelector { get; }
    
    public Func<string, string, bool>? CustomEqualityComparer { get; }

    
    public TagComparison(
        string osmKey,
        Func<TDataItem, string?> expectedValueSelector,
        Func<string, string, bool>? customEqualityComparer = null)
    {
        OsmKey = osmKey;
        ExpectedValueSelector = expectedValueSelector;
        CustomEqualityComparer = customEqualityComparer;
    }
}