namespace Osmalyzer;

/// <summary>
/// Marks an <see cref="Analyzer"/> as disabled, preventing it from running.
/// An optional reason can be specified for reporting why it is disabled.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class DisabledAnalyzerAttribute : Attribute
{
    /// <summary>
    /// Optional human-readable reason why the analyzer is disabled.
    /// </summary>
    public string? Reason { get; }

    
    /// <summary>
    /// Initializes a new instance of the <see cref="DisabledAnalyzerAttribute"/>.
    /// </summary>
    /// <param name="reason">Optional reason explaining why the analyzer is disabled.</param>
    public DisabledAnalyzerAttribute(string? reason = null)
    {
        Reason = reason;
    }
}