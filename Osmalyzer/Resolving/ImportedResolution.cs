using System;
using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer;

/// <summary>
/// This is the instance loaded/imported for a previously-resolved problem by the user.
/// It's expected that this issue will not appear as a problem anymore, although presentation is up to UI.
/// </summary>
public class ImportedResolution : Resolution
{
    private ImportedResolution(Resolvable problem, DateTime timestamp, string comment)
        : base(problem, timestamp, comment)
    {
    }
    
    
    /// <summary>
    /// Loads a previously-stored resolution from its dumped data.
    /// This creates a new instance, it's up to whoever uses these to match these.
    /// </summary>
    [Pure]
    public static ImportedResolution? Import(List<string?> values, IEnumerable<IAnalyzerWithResolutions> analyzers)
    {
        int dataRevision = int.Parse(values[0]!); // e.g. 1

        if (dataRevision > Resolvable.revision)
            throw new NotImplementedException();
        
        if (values.Count != 8)
            throw new Exception();

        int version = int.Parse(values[0]!); // e.g. 1

        string analyzerID = values[2]!; // e.g. "seb_bank_locator"

        IAnalyzerWithResolutions? analyzer = analyzers.FirstOrDefault(a => a.ResolutionAnalyzerID == analyzerID);
        if (analyzer == null) // we no longer have it?
            return null;

        string issueID = values[3]!; // e.g. "far_apart"

        string? itemData = values[4]; // e.g. some hash of defining values
        
        string? elementData = values[5]; // e.g. some hash of tags 

        Resolvable resolvable;

        if (itemData != null && elementData != null)
            resolvable = new ResolvableItemElementPair(version, analyzer, issueID, itemData, elementData);
        else if (itemData != null)
            resolvable = new ResolvableItem(version, analyzer, issueID, itemData);
        else if (elementData != null)
            resolvable = new ResolvableElement(version, analyzer, issueID, elementData);
        else
            throw new Exception();
        
        DateTime timestamp = new DateTime(long.Parse(values[6]!)); 
        
        string comment = values[7]!; // e.g. "Official data has wrong coordinate, this location is correct"

        return new ImportedResolution(
            resolvable,
            timestamp,
            comment
        );
    }
}