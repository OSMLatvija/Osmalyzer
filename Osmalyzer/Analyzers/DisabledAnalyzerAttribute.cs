namespace Osmalyzer;

[AttributeUsage(AttributeTargets.Class)]
public class DisabledAnalyzerAttribute : Attribute
{
    public string? Reason { get; }

    
    public DisabledAnalyzerAttribute(string? reason = null)
    {
        Reason = reason;
    }
}