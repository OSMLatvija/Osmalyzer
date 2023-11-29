using JetBrains.Annotations;

namespace Osmalyzer;

/// <summary>
/// A reported issue that can be resolved by the user by choosing to "ignore" this issue.
/// That way, it can be reported/drawn as "resolved" in future reports rather than an error.
/// This is added to <see cref="ReportEntry"/>s that support this.
/// Having this means the report output should present the user a UI option to resolve this.
/// </summary>
public abstract class Resolvable
{
    /// <summary>
    /// Revisioning against structural changes, like field changes.
    /// </summary>
    public const int revision = 1;
    
    /// <summary>
    /// Versioning against future changes in case we need to "reset" this while still having the same data,
    /// that is, any previously-resolved issue would no longer match the revision number and get skipped.
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// The analyzer that supplies this resolution, uniquely identified.
    /// This disambiguates between cases where the same element or item or pair can be reported by different analyzers for different problems.
    /// </summary>
    public IAnalyzerWithResolutions Analyzer { get; }

    /// <summary>
    /// Unique stable ID for the problem/issue or whatever "group" this specific resolution belong to.
    /// This disambiguates between cases where the same element or item or pair can be reported for multiple problems.
    /// </summary>
    public string IssueID { get; }

    
    protected Resolvable(int version, IAnalyzerWithResolutions analyzer, string issueID)
    {
        Version = version;
        Analyzer = analyzer;
        IssueID = issueID;
    }

    
    /// <summary>
    /// Do these represent the same problem?
    /// Use this instead of equality checks, because loaded/imported instances are not the same as the instances analyzers create at runtime.
    /// </summary>
    [Pure]
    public bool Matches(Resolvable other)
    {
        if (GetType() != other.GetType())
            return false;
        
        return 
            Version == other.Version && 
            Analyzer.ResolutionAnalyzerID == other.Analyzer.ResolutionAnalyzerID && 
            IssueID == other.IssueID &&
            ChildMatches(other);
    }


    protected abstract bool ChildMatches(Resolvable other);
}