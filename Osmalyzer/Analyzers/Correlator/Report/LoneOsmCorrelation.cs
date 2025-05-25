namespace Osmalyzer;

public class LoneOsmCorrelation : Correlation
{
    public OsmElement OsmElement { get; set; }

    
    public LoneOsmCorrelation(OsmElement osmElement)
    {
        OsmElement = osmElement;
    }
}