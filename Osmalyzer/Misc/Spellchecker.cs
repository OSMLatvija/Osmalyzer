﻿namespace Osmalyzer;

public class Spellchecker
{
    private readonly ISpellcheckProvider[] _providers;

    
    public Spellchecker(params ISpellcheckProvider[] providers)
    {
        _providers = providers;
    }

    
    [Pure]
    public SpellcheckResult Check(string text)
    {
        char[] punctuation = text.Where(char.IsPunctuation).Distinct().ToArray();
        
        IEnumerable<string> words = text.Split().Select(x => x.Trim(punctuation)).Where(w => w != "");

        List<Misspelling>? misspellings = null;

        foreach (string word in words)
        {
            bool ok = false;
            
            foreach (ISpellcheckProvider provider in _providers)
            {
                if (provider.Spell(word))
                {
                    ok = true; // accepted spelling from at least one provider == ok
                    break;
                }
            }

            if (!ok)
            {
                if (misspellings == null)
                    misspellings = new List<Misspelling>();

                misspellings.Add(new Misspelling(word));
            }
        }
        
        return 
            misspellings == null ? 
                OkaySpellcheckResult.Instance : 
                new MisspelledSpellcheckResult(misspellings);
    }
}


public abstract class SpellcheckResult
{
}

public class OkaySpellcheckResult : SpellcheckResult
{
    public static OkaySpellcheckResult Instance { get; } = new OkaySpellcheckResult();
    private OkaySpellcheckResult() { }
}

public class MisspelledSpellcheckResult : SpellcheckResult
{
    public List<Misspelling> Misspellings { get; }

    
    public MisspelledSpellcheckResult(List<Misspelling> misspellings)
    {
        Misspellings = misspellings;
    }
}

public class Misspelling
{
    public string Word { get; }

    
    public Misspelling(string word)
    {
        Word = word;
    }
}


public interface ISpellcheckProvider
{
    [Pure]
    public bool Spell(string word);
}