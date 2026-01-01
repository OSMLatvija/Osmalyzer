namespace Osmalyzer;

public static class SuggestedActionApplicator
{
    public static void Apply(OsmMasterData data, List<SuggestedAction> changes)
    {
        foreach (SuggestedAction change in changes)
        {
            switch (change)
            {
                case OsmSetValueSuggestedAction setValue:
                    setValue.Element.SetValue(
                        setValue.Key,
                        setValue.Value
                    );
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(change));
            }
        }
    }
}