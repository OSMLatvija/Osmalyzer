using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Osmalyzer;

[UsedImplicitly]
public class LatvianCustomDictionaryAnalysisData : AnalysisData, ISpellcheckProvider
{
    public override string Name => "Latvian custom spellcheck dictionary";

    public override string ReportWebLink => "";
    
    public override bool NeedsPreparation => false;
    
    protected override string DataFileIdentifier => "";


    private HashSet<string> _customWordList = null!; // only null until created


    protected override void Download()
    {
        string dataFileName = @"data/latvian custom dictionary.tsv";

        if (!File.Exists(dataFileName))
            dataFileName = @"../../../../" + dataFileName; // "exit" Osmalyzer\bin\Debug\net_.0\ folder and grab it from root data\

        _customWordList = new HashSet<string>();
        
        foreach (string line in File.ReadAllLines(dataFileName))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            
            if (line.StartsWith("//")) // comments
                continue;

            string[] splits = line.Split('\t');
            
            // 1 is the word
            // 2 is (optional) comment

            _customWordList.Add(splits[0]);
        }
    }

    protected override void DoPrepare()
    {
        // Not doing preparation
        throw new Exception();
    }

    
    [Pure]
    public bool Spell(string word)
    {
        return _customWordList.Contains(word);
    }
}