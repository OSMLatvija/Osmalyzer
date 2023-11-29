namespace Osmalyzer;

public class ResolvableItemElementPair : Resolvable, IResolvableWithItem, IResolvableWithElement
{
    public string Item { get; }

    public string Element { get; }


    public ResolvableItemElementPair(int version, IAnalyzerWithResolutions analyzer, string issueID, string item, string element)
        : base(version, analyzer, issueID)
    {
        Item = item;
        Element = element;
    }

    
    protected override bool ChildMatches(Resolvable other)
    {
        ResolvableItemElementPair otherCast = (ResolvableItemElementPair)other;

        return
            Item == otherCast.Item &&
            Element == otherCast.Element;
    }
}