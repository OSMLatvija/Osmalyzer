namespace Osmalyzer;

public static class SuggestedActionApplicator
{
    public static OsmData Apply(OsmData data, List<SuggestedAction> changes, bool temporary)
    {
        if (temporary)
        {
            // Make a deep data copy
            data = data.Copy();
            
            // "Remap" elements in suggested actions from originals to the copies
            for (int i = 0; i < changes.Count; i++)
            {
                SuggestedAction change = changes[i];
                
                switch (change)
                {
                    case OsmSetValueSuggestedAction setValue:
                    {
                        OsmElement copiedElement = data.GetElementById(setValue.Element.ElementType, setValue.Element.Id);
                        changes[i] = new OsmSetValueSuggestedAction(copiedElement, setValue.Key, setValue.Value);
                        break;
                    }

                    case OsmRemoveKeySuggestedAction removeKey:
                    {
                        OsmElement copiedElement = data.GetElementById(removeKey.Element.ElementType, removeKey.Element.Id);
                        changes[i] = new OsmRemoveKeySuggestedAction(copiedElement, removeKey.Key);
                        break;
                    }

                    default:
                        throw new ArgumentOutOfRangeException(nameof(change));
                }
            }
        }

        foreach (SuggestedAction change in changes)
        {
            switch (change)
            {
                case OsmSetValueSuggestedAction setValue:
                    setValue.Element.SetValue(setValue.Key, setValue.Value);
                    break;
                
                case OsmRemoveKeySuggestedAction removeKey:
                    removeKey.Element.RemoveKey(removeKey.Key);
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

        OsmData osmData = Apply(osmMasterData, suggestedChanges, true);

        OsmChange change = osmData.GetChanges();
        
        string xml = change.ToXml();
        
        if (!Directory.Exists("Suggested changes"))
            Directory.CreateDirectory("Suggested changes");
        string fileName = Path.Combine("Suggested changes", analyzer.Name + ".osc");
        File.WriteAllText(fileName, xml);

        Console.WriteLine(change.Actions.Count + " suggested changes for " + analyzer.Name + " written to " + fileName);
    }
}