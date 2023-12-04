using JetBrains.Annotations;

namespace Osmalyzer;

public static class FuzzyNameMatcher
{
    [Pure]
    public static bool Matches(OsmElement element, string key, string targetName)
    {
        if (!element.HasKey(key))
            return false;
        
        return Matches(
            element.GetValue(key)!,
            targetName
        );
    }
    
    [Pure]
    public static bool Matches(string name1, string name2)
    {
        name1 = name1.ToLower().Trim();
        name2 = name2.ToLower().Trim();
        
        if (name1.Contains(name2))
            return true;
        
        if (name2.Contains(name1))
            return true;

        return false;
    }
}