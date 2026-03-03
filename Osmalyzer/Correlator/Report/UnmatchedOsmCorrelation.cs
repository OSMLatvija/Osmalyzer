namespace Osmalyzer;

public class UnmatchedOsmCorrelation : Correlation
{
    public OsmElement OsmElement { get; set; }

    
    public UnmatchedOsmCorrelation(OsmElement osmElement)
    {
        OsmElement = osmElement;
    }
}