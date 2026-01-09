using System.Diagnostics;

namespace Osmalyzer;

public static class SuggestedActionApplicator
{
    public static OsmData Apply(OsmData data, List<SuggestedAction> changes, bool temporary)
    {
        if (temporary)
        {
            // Make a deep data copy
            Stopwatch stopwatch = Stopwatch.StartNew();
            data = data.Copy();
            Console.WriteLine("-> -> Closed OsmData in " + stopwatch.ElapsedMilliseconds + " ms.");
            
            // "Remap" elements in suggested actions from originals to the copies
            stopwatch.Restart();
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
                    
                    case OsmChangeKeySuggestedAction changeKey:
                    {
                        OsmElement copiedElement = data.GetElementById(changeKey.Element.ElementType, changeKey.Element.Id);
                        changes[i] = new OsmChangeKeySuggestedAction(copiedElement, changeKey.OldKey, changeKey.NewKey, changeKey.Value);
                        break;
                    }
                    
                    case OsmCreateElementAction createElement:
                    {
                        OsmElement copiedElement = data.GetElementById(createElement.Element.ElementType, createElement.Element.Id);
                        changes[i] = new OsmCreateElementAction(copiedElement);
                        break;
                    }

                    default:
                        throw new ArgumentOutOfRangeException(nameof(change));
                }
            }
            Console.WriteLine("-> -> Remapped suggested actions in " + stopwatch.ElapsedMilliseconds + " ms.");
        }

        Stopwatch mainStopwatch = Stopwatch.StartNew();
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
                
                case OsmChangeKeySuggestedAction changeKey:
                    changeKey.Element.RemoveKey(changeKey.OldKey);
                    changeKey.Element.SetValue(changeKey.NewKey, changeKey.Value);
                    break;
                
                case OsmCreateElementAction:
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(change));
            }
        }
        Console.WriteLine("-> -> Applied suggested actions in " + mainStopwatch.ElapsedMilliseconds + " ms.");

        return data;
    }

    public static OsmChange? ApplyAndProposeXml(OsmData osmMasterData, List<SuggestedAction> suggestedChanges, Analyzer analyzer, string? caption = null)
    {
        if (suggestedChanges.Count == 0)
            return null;

        Stopwatch stopwatch = Stopwatch.StartNew();
        OsmData osmData = Apply(osmMasterData, suggestedChanges, true);
        Console.WriteLine("-> Applied " + suggestedChanges.Count + " suggested changes in " + stopwatch.ElapsedMilliseconds + " ms.");

        stopwatch.Restart();
        OsmChange change = osmData.GetChanges();
        Console.WriteLine("-> Generated OsmChange in " + stopwatch.ElapsedMilliseconds + " ms.");
        
        stopwatch.Restart();
        
        string xml = change.ToXml();
        
        if (!Directory.Exists("Suggested changes"))
            Directory.CreateDirectory("Suggested changes");
        
        string fileName =
            caption != null ?
                Path.Combine("Suggested changes", analyzer.Name + " - " + caption + ".osc") :
                Path.Combine("Suggested changes", analyzer.Name + ".osc");
        
        File.WriteAllText(fileName, xml);

        Console.WriteLine("-> Wrote suggested changes as XML in " + stopwatch.ElapsedMilliseconds + " ms.");
        
        //Console.WriteLine(change.Actions.Count + " suggested changes for " + analyzer.Name + " written to " + fileName);
        
        return change;
    }

    public static void ExplainForReport(List<SuggestedAction> changes, Report report, object groupId)
    {
        report.AddGroup(
            groupId,
            "Proposed changes",
            "These are automatically proposed changes to the OSM data.",
            "No changes are proposed."
        );
        
        if (changes.Count == 0)
            return;

        int keyRemovals = 0;
        int keyAdditions = 0;
        int keyModifications = 0;
        int keyChanges = 0;
        HashSet<OsmElement> elements = [ ];
        HashSet<OsmNode> nodes = [ ];
        HashSet<OsmWay> ways = [ ];
        HashSet<OsmRelation> relations = [ ];
        HashSet<string> keys = [ ];
        HashSet<string> keysToRemove = [ ];
        HashSet<string> keysToAdd = [ ];
        HashSet<string> keysToModify = [ ];
        HashSet<string> keysToChange = [ ];
        Dictionary<string, List<string?>> perKeyChanges = [ ];
        int nodeCreations = 0;
        
        foreach (SuggestedAction change in changes)
        {
            elements.Add(change.Element);

            switch (change.Element)
            {
                case OsmNode osmNode:
                    nodes.Add(osmNode);
                    break;
                case OsmRelation osmRelation:
                    relations.Add(osmRelation);
                    break;
                case OsmWay osmWay:
                    ways.Add(osmWay);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch (change)
            {
                case OsmRemoveKeySuggestedAction removeKey:
                    keyRemovals++;
                    keys.Add(removeKey.Key);
                    keysToRemove.Add(removeKey.Key);
                    perKeyChanges.TryAdd(removeKey.Key, [ ]);
                    perKeyChanges[removeKey.Key].Add(null);
                    break;
                
                case OsmSetValueSuggestedAction setValue:
                    keys.Add(setValue.Key);
                    perKeyChanges.TryAdd(setValue.Key, [ ]);
                    perKeyChanges[setValue.Key].Add(setValue.Value);
                    if (change.Element.HasKey(setValue.Key))
                    {
                        keyModifications++;
                        keysToModify.Add(setValue.Key);
                    }
                    else
                    {
                        keyAdditions++;
                        keysToAdd.Add(setValue.Key);
                    }

                    break;
                
                case OsmChangeKeySuggestedAction changeKey:
                    keyChanges++;
                    keys.Add(changeKey.OldKey);
                    keys.Add(changeKey.NewKey);
                    keysToChange.Add(changeKey.OldKey);
                    perKeyChanges.TryAdd(changeKey.OldKey, [ ]);
                    perKeyChanges[changeKey.OldKey].Add(null);
                    perKeyChanges.TryAdd(changeKey.NewKey, [ ]);
                    perKeyChanges[changeKey.NewKey].Add(changeKey.Value);
                    break;
                
                case OsmCreateElementAction createElement:
                    switch (createElement.Element)
                    {
                        case OsmNode: nodeCreations++; break;
                        case OsmWay: break;
                        case OsmRelation: break;
                        default: throw new ArgumentOutOfRangeException();
                    }
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(change));
            }
        }

        AddLine(changes.Count + " changes are proposed:");
        
        if (nodeCreations > 0) AddLine(nodeCreations + " node creations");
        
        if (keyAdditions > 0) AddLine(keyAdditions + " key additions");
        if (keyModifications > 0) AddLine(keyModifications + " key modifications");
        if (keyRemovals > 0) AddLine(keyRemovals + " key removals");
        if (keyChanges > 0) AddLine(keyChanges + " key changes");

        if (elements.Count == nodes.Count)
        {
            AddLine(nodes.Count + " nodes are affected.");
        }
        else if (elements.Count == ways.Count)
        {
            AddLine(ways.Count + " ways are affected.");
        }
        else if (elements.Count == relations.Count)
        {
            AddLine(relations.Count + " relations are affected.");
        }
        else
        {
            AddLine(elements.Count + " elements are affected:");
            if (nodes.Count > 0) AddLine(" - " + nodes.Count + " nodes");
            if (ways.Count > 0) AddLine(" - " + ways.Count + " ways");
            if (relations.Count > 0) AddLine(" - " + relations.Count + " relations");
        }

        if (keys.Count == keysToAdd.Count)
        {
            AddLine(keysToAdd.Count + " unique keys are added.");
        }
        else if (keys.Count == keysToModify.Count)
        {
            AddLine(keysToModify.Count + " unique keys are modified.");
        }
        else if (keys.Count == keysToRemove.Count)
        {
            AddLine(keysToRemove.Count + " unique keys are removed.");
        }
        else
        {
            if (keys.Count > 0) AddLine(keys.Count + " unique keys are affected.");
            if (keysToAdd.Count > 0) AddLine(keysToAdd.Count + " unique keys are added");
            if (keysToModify.Count > 0) AddLine(keysToModify.Count + " unique keys are modified");
            if (keysToRemove.Count > 0) AddLine(keysToRemove.Count + " unique keys are removed");
            if (keysToChange.Count > 0) AddLine(keysToChange.Count + " unique keys are changed");
        }

        if (perKeyChanges.Count > 0 && perKeyChanges.Count <= 10)
        {
            foreach (KeyValuePair<string, List<string?>> keyChange in perKeyChanges)
            {
                Dictionary<string, int> valueCounts = [ ];
                
                foreach (string? value in keyChange.Value)
                {
                    valueCounts.TryAdd(value ?? "", 0);
                    valueCounts[value ?? ""]++;
                }

                if (valueCounts.Count == 1)
                {
                    string onlyValue = valueCounts.Keys.First();

                    AddLine($"Key `{keyChange.Key}` has all {valueCounts[onlyValue]} values changed to " + (onlyValue == "" ? "<removed>" : "`" + onlyValue + "`"));
                }
                else if (valueCounts.Count < 10)
                {
                    string changesList = string.Join(", ", valueCounts.OrderByDescending(c => c.Value).Select(kv => kv.Value + " × " + (kv.Key == "" ? "<removed>" : "`" + kv.Key + "`")));

                    AddLine($"Key `{keyChange.Key}` has {valueCounts.Count} values changed: {changesList}");
                }
                else
                {
                    if (valueCounts.Count == keyChange.Value.Count)
                    {
                        AddLine($"Key `{keyChange.Key}` has {valueCounts.Count} all unique values changed.");
                    }
                    else
                    {
                        AddLine($"Key `{keyChange.Key}` has {valueCounts.Count} different values changed.");
                    }
                }
            }
        }
        
        return;
        

        void AddLine(string line)
        {
            report.AddEntry(
                groupId,
                new GenericReportEntry(
                    line
                )
            );
        }
    }
}