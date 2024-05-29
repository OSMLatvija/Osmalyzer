namespace Osmalyzer;

public class UnmatchedCorrelation : Correlation
{
    public OsmElement OsmElement { get; set; }

    
    public UnmatchedCorrelation(OsmElement osmElement)
    {
        OsmElement = osmElement;
    }
}