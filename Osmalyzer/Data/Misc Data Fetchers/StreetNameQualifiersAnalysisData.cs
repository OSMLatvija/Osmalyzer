using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Osmalyzer;

[UsedImplicitly]
public class StreetNameQualifiersAnalysisData : AnalysisData
{
    public override string Name => "Street name generic qualifiers/descriptors";

    public override string? ReportWebLink => null;
    
    public override bool NeedsPreparation => false;
    
    protected override string DataFileIdentifier => "";

    private readonly string[] locales = new string[] { "ru", "en" };

    public Dictionary<string, Dictionary<string, List<string>>> Names { get; private set; } = null!; // only null until downloaded


    protected override void Download()
    {
        Names = new Dictionary<string, Dictionary<string, List<string>>>();
        
        string dataFileName = @"data/street name qualifiers.tsv";

        if (!File.Exists(dataFileName))
            dataFileName = @"../../../../" + dataFileName; // "exit" Osmalyzer\bin\Debug\net_.0\ folder and grab it from root data\
            
        string[] lines = File.ReadAllLines(dataFileName, Encoding.UTF8);

        foreach (string line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("//"))
            {
                string[] splits = line.Split('\t');
                
                if (splits.Length != locales.Length + 1)
                    throw new Exception("Incorrect number of locales in '" + dataFileName + "' file in line: " + line);
                
                Dictionary<string, List<string>> variants = new Dictionary<string, List<string>>();
                for (int i = 0; i < locales.Length; i++)
                {
                    variants.Add(locales[i], splits[i+1].Split(';').ToList());
                }
                Names.Add(splits[0], variants);
            }
        }
    }

    protected override void DoPrepare()
    {
        // Not doing preparation
        throw new Exception();
    }
}