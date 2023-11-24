namespace Osmalyzer;

public class MatchDistanceParamater : CorrelatorParamater
{
    public int Distance { get; }

        
    public MatchDistanceParamater(int distance)
    {
        Distance = distance;
    }
}