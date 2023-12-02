using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace Osmalyzer;

public static class AnalyzerGroups
{
    public static AnalyzerGroup Shop { get; } = new AnalyzerGroup("Shops");
    public static AnalyzerGroup Bank { get; } = new AnalyzerGroup("Banks");
    public static AnalyzerGroup Road { get; } = new AnalyzerGroup("Roads");
    public static AnalyzerGroup PublicTransport { get; } = new AnalyzerGroup("Public Transport");
    public static AnalyzerGroup Misc { get; } = new AnalyzerGroup("Miscellaneous");


    [Pure]
    public static IEnumerable<AnalyzerGroup> GetAllGroups()
    {
        return typeof(AnalyzerGroups)
               .GetProperties(BindingFlags.Static | BindingFlags.Public)
               .Where(p => p.PropertyType == typeof(AnalyzerGroup))
               .Select(p => (AnalyzerGroup)p.GetValue(null)!);
    }
}