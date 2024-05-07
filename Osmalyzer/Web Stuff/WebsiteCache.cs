using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer;

public static class WebsiteCache
{
    private static readonly List<(string url, string content)> _cachedWebsites = new List<(string url, string content)>();

    
    [Pure]
    public static bool IsCached(string url)
    {
        return _cachedWebsites.Any(cw => cw.url == url);
    }

    [Pure]
    public static string GetCached(string url)
    {
        return _cachedWebsites.FirstOrDefault(cw => cw.url == url).content;
    }

    public static void Cache(string url, string content)
    {
        _cachedWebsites.Add((url, content));
    }
}