namespace Osmalyzer;

public class ResolvableElement : Resolvable, IResolvableWithElement
{
    public string Element { get; }


    public ResolvableElement(int version, IAnalyzerWithResolutions analyzer, string issueID, string element)
        : base(version, analyzer, issueID)
    {
        Element = element;
    }

    
    protected override bool ChildMatches(Resolvable other)
    {
        ResolvableElement otherCast = (ResolvableElement)other;

        return Element == otherCast.Element;
    }
}