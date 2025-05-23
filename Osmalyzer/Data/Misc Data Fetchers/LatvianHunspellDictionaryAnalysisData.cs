﻿using WeCantSpell.Hunspell;

namespace Osmalyzer;

[UsedImplicitly]
public class LatvianHunspellDictionaryAnalysisData : AnalysisData, ISpellcheckProvider
{
    public override string Name => "Latvian Hunspell spellcheck dictionary";

    public override string ReportWebLink => "https://dict.dv.lv/download.php?prj=lv";
    
    public override bool NeedsPreparation => true;
    
    protected override string DataFileIdentifier => "";


    private WordList _dictionary = null!; // only null until prepared


    protected override void Download()
    {
        // https://dict.dv.lv/download.php?prj=lv
        // todo: do we actually want to?
    }

    protected override void DoPrepare()
    {
        string dataFileName = @"data/latvian spellcheck dictionary.zip";

        if (!File.Exists(dataFileName))
            dataFileName = @"../../../../" + dataFileName; // "exit" Osmalyzer\bin\Debug\net_.0\ folder and grab it from root data\
        
        ZipHelper.ExtractZipFile(
            dataFileName,
            Path.GetFullPath("LSD") // lol
        );

        
        _dictionary = WordList.CreateFromFiles(@"LSD/lv_LV.dic", "LSD/lv_LV.aff");
        // todo: hypen data
    }

    
    [Pure]
    public bool Spell(string word)
    {
        return _dictionary.Check(word);
    }
}