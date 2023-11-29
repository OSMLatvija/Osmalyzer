using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Osmalyzer;

/// <summary>
/// This is the instance created by the analyzer proposing this potential resolution.
/// </summary>
public class RuntimeResolution : Resolution
{
    public RuntimeResolution(Resolvable problem, DateTime timestamp, string comment)
    {
        Problem = problem;
        Timestamp = timestamp;
        Comment = comment;
    }
    

    /// <summary>
    /// Provides a list of entries that can be stored into whatever storage used to permanently record and later retrieve user resolutions.
    /// </summary>
    [Pure]
    public List<string?> GetExportableData()
    {
        List<string?> exports = new List<string?>();

        exports.Add(Resolvable.revision.ToString()); // e.g. 1
        
        exports.Add(Problem.Version.ToString()); // e.g. 1
        
        exports.Add(Problem.Analyzer.ResolutionAnalyzerID); // e.g. "seb_bank_locator"
        
        exports.Add(Problem.IssueID); // e.g. "far_apart"
        
        exports.Add(Problem is IResolvableWithItem itemProblem ? itemProblem.Item : null); // e.g. some hash of defining values
        
        exports.Add(Problem is IResolvableWithElement elementProblem ? elementProblem.Element : null); // e.g. some hash of tags 
        
        exports.Add(Timestamp.Ticks.ToString()); 
        
        exports.Add(Comment); // e.g. "Official data has wrong coordinate, this location is correct"

        return exports;
    }
}