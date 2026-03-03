namespace Osmalyzer;

public class Spawn
{
    public List<SuggestedAction> Additions { get; }


    public Spawn(List<SuggestedAction> additions)
    {
        Additions = additions;
    }
}