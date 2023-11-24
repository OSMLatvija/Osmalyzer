namespace Osmalyzer;

public class LoneCorrelation : Correlation
{
    public OsmElement OsmElement { get; set; }

    
    public LoneCorrelation(OsmElement osmElement)
    {
        OsmElement = osmElement;
    }
}