namespace Osmalyzer;

public class AnalyzerGroup
{
    public static AnalyzerGroup Shops { get; } = new AnalyzerGroup("Shops");
    public static AnalyzerGroup Banks { get; } = new AnalyzerGroup("Banks");
    public static AnalyzerGroup Roads { get; } = new AnalyzerGroup("Roads");
    public static AnalyzerGroup PublicTransport { get; } = new AnalyzerGroup("Public Transport");
    public static AnalyzerGroup ParcelLockers { get; } = new AnalyzerGroup("Parcel Lockers");
    public static AnalyzerGroup Restaurants { get; } = new AnalyzerGroup("Restaurants");
    public static AnalyzerGroup Validation { get; } = new AnalyzerGroup("Validation");
    public static AnalyzerGroup POIs { get; } = new AnalyzerGroup("POIs");
    public static AnalyzerGroup Miscellaneous { get; } = new AnalyzerGroup("Miscellaneous");
    
    
    public string Title { get; }

    
    private AnalyzerGroup(string title)
    {
        Title = title;
    }
}