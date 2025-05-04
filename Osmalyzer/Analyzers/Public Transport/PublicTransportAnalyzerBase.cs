namespace Osmalyzer;

public abstract class PublicTransportAnalyzerBase : Analyzer
{
    protected static Dictionary<string, string> CleanedStopNameCache { get; } = [ ];
    // In base class because generic inheritors won't share this, i.e. PTA<RS>.cache != PTA<JAP>.cache, but we want to share cache
}