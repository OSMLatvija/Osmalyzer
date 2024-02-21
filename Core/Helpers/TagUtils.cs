using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer;

public static class TagUtils
{
    public static List<string> SplitValue(string value)
    {
        string[] splits = value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        return splits.Select(s => s.Trim()).ToList();

        // todo: remove/detect duplicates
    }

    [Pure]
    public static bool ValuesMatch(string v1, string v2)
    {
        v1 = v1.Trim();
        v2 = v2.Trim();

        if (v1 == v2)
            return true;

        if (v1.Contains(';') && v2.Contains(';'))
        {
            // e.g. crossing:markings - zebra;dots vs dots;zebra.

            string[] v1s = v1.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
            string[] v2s = v1.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();

            if (v1s.Length != v2s.Length)
                return false;

            foreach (string v1p in v1s) // we trimmed duplicates and matched count, so doesn't matter which collection we iterate against the other
                if (!v2s.Contains(v1p))
                    return false;

            return true;
        }
                    
        return false;
    }
}