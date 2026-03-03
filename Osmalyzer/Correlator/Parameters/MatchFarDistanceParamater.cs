namespace Osmalyzer;

public class MatchFarDistanceParamater : CorrelatorParamater
{        
    public int FarDistance { get; }

        
    public MatchFarDistanceParamater(int farDistance)
    {
        FarDistance = farDistance;
    }
}