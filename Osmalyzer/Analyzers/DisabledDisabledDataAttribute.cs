namespace Osmalyzer;

/// <summary>
/// Marks an <see cref="AnalysisData"/> type as disabled, preventing analyzers that require it from running.
/// Use this when the external data source is temporarily unavailable or otherwise unusable.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class DisabledDataAttribute : Attribute
{
    /// <summary>
    /// Optional human-readable reason why this data is disabled.
    /// </summary>
    public string? Reason { get; }

    
    /// <summary>
    /// Initializes a new instance of the <see cref="DisabledDataAttribute"/>.
    /// </summary>
    /// <param name="reason">Optional reason explaining why the data is disabled.</param>
    public DisabledDataAttribute(string? reason = null)
    {
        Reason = reason;
    }
}