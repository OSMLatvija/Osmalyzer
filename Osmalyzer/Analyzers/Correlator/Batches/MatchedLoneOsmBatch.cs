namespace Osmalyzer;

public class MatchedLoneOsmBatch : CorrelatorBatch
{
    public bool AsProblem { get; }

    
    public MatchedLoneOsmBatch(bool asProblem)
    {
        AsProblem = asProblem;
    }
}