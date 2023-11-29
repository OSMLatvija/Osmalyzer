namespace Osmalyzer;

public class ResolvableItem : Resolvable, IResolvableWithItem
{
    public string Item { get; }


    public ResolvableItem(int version, IAnalyzerWithResolutions analyzer, string issueID, string item)
        : base(version, analyzer, issueID)
    {
        Item = item;
    }

    
    protected override bool ChildMatches(Resolvable other)
    {
        ResolvableItem otherCast = (ResolvableItem)other;

        return Item == otherCast.Item;
    }
}