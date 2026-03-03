namespace Osmalyzer;

public class Validation
{
    public List<SuggestedAction> Changes { get; }


    public Validation(List<SuggestedAction> changes)
    {
        Changes = changes;
    }
}