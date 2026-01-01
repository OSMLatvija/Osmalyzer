namespace Osmalyzer;

public static class SuggestedActionApplicator
{
    public static OsmData Apply(OsmData data, List<SuggestedAction> changes)
    {
        // TODO:
        // the problem is that this works on shared data directly, so I am modifying the "original" data before other analyzers have run,
        // so if they read the same element, they will see the modified version already
        
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

        return data;
    }

    public static void ApplyAndProposeXml(OsmData osmMasterData, List<SuggestedAction> suggestedChanges, Analyzer analyzer)
    {
        if (suggestedChanges.Count == 0)
            return;
        
        Apply(osmMasterData, suggestedChanges);
        
        OsmChange change = osmMasterData.GetChanges();
        
        string xml = change.ToXml();
        
        if (!Directory.Exists("Suggested changes"))
            Directory.CreateDirectory("Suggested changes");
        File.WriteAllText("Suggested changes/" + analyzer.Name + ".osc", xml);
    }
}