using System;
using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer;

public static class TagUtils
{
    public static List<string> SplitValue(string value)
    {
        string[] splits = value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        return splits.Select(s => s.Trim()).ToList();

        // todo: remove/detect duplicates
    }
}