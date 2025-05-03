using System.Reflection;

namespace Osmalyzer;

public static class AnalyzerGroups
{
    public static AnalyzerGroup Shop { get; } = new AnalyzerGroup("Shops");
    public static AnalyzerGroup Bank { get; } = new AnalyzerGroup("Banks");
    public static AnalyzerGroup Road { get; } = new AnalyzerGroup("Roads");
    public static AnalyzerGroup PublicTransport { get; } = new AnalyzerGroup("Public Transport");
    public static AnalyzerGroup Misc { get; } = new AnalyzerGroup("Miscellaneous");
    public static AnalyzerGroup ParcelLocker { get; } = new AnalyzerGroup("Parcel Lockers");

    public static AnalyzerGroup Restaurants { get; } = new AnalyzerGroup("Restaurants");


    [Pure]
    public static IEnumerable<AnalyzerGroup> GetAllGroups()
    {
        return typeof(AnalyzerGroups)
               .GetProperties(BindingFlags.Static | BindingFlags.Public)
               .Where(p => p.PropertyType == typeof(AnalyzerGroup))
               .Select(p => (AnalyzerGroup)p.GetValue(null)!);
    }
}