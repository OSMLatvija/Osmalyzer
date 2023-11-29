using System.Collections.Generic;
using System.IO;

namespace Osmalyzer;

public class DebugResolutionImporter : ResolutionImporter
{
    protected override IEnumerable<List<string?>> ChildImportData()
    {
        string[] lines = File.ReadAllLines("test.tsv");

        foreach (string line in lines)
        {
            string[] split = line.Split('\t');

            yield return new List<string?>()
            {
                split[0], // revision
                split[1], // version
                split[2], // analyzer ID
                split[3], // issue ID
                split[4] != "" ? split[4] : null, // item data
                split[5] != "" ? split[5] : null, // element data
                split[6], // timestamp
                split[7] // comment
            };
        }
    }
}