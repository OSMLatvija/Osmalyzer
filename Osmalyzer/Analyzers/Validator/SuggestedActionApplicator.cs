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
                        OsmElement copiedElement = data.GetElementById(setValue.Element.ElementType, setValue.Element.Id);
                        changes[i] = new OsmSetValueSuggestedAction(copiedElement, setValue.Key, setValue.Value);
                        
                        switch (setValue.Element.ElementType)
                        {
                            case OsmElement.OsmElementType.Node:
                                OsmNode copiedNode = data.GetNodeById(setValue.Element.Id);
                                changes[i] = new OsmSetValueSuggestedAction(copiedNode, setValue.Key, setValue.Value);
                                break;

                            case OsmElement.OsmElementType.Way:
                                OsmWay copiedWay = data.GetWayById(setValue.Element.Id);
                                changes[i] = new OsmSetValueSuggestedAction(copiedWay, setValue.Key, setValue.Value);
                                break;

                            case OsmElement.OsmElementType.Relation:
                                OsmRelation copiedRelation = data.GetRelationById(setValue.Element.Id);
                                changes[i] = new OsmSetValueSuggestedAction(copiedRelation, setValue.Key, setValue.Value);
                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        break;

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