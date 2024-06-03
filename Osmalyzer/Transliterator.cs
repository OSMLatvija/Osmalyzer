namespace Osmalyzer;

public static class Transliterator
{
    [Pure]
    public static string TransliterateFromLvToRu(string name)
    {
        string translit = "";
        
        foreach (char c in name)
        {
            string newC = TranslitLatinToCyrillicChar(c);

            translit += newC;
        }

        return translit;
    }

    
    [Pure]
    private static string TranslitLatinToCyrillicChar(char c)
    {
        bool lower = char.IsLower(c);

        string newChar = TranslitLatinToCyrillicLowerChar(c);
        
        return lower ? newChar : newChar.ToUpper();
    }

    [Pure]
    private static string TranslitLatinToCyrillicLowerChar(char c)
    {
        return char.ToLower(c) switch
        {
            // Latin
            
            'a' => "а",
            'b' => "б",
            'c' => "ц",
            'd' => "д",
            'e' => "е",
            'f' => "ф",
            'g' => "г",
            'h' => "х",
            'i' => "и",
            'j' => "й",
            'k' => "к",
            'l' => "л",
            'm' => "м",
            'n' => "н",
            'o' => "о",
            'p' => "п",
            'r' => "р",
            's' => "с",
            't' => "т",
            'u' => "у",
            'v' => "в",
            'w' => "в",
            'z' => "з",
            
            // Non-Latvian
            'q' => "к",
            'x' => "кс",
            'y' => "й",

            // Latvian
            'ā' => "а",
            'č' => "ч",
            'ē' => "е",
            'ģ' => "г",
            'ī' => "и",
            'ķ' => "к",
            'ļ' => "л",
            'ņ' => "н",
            'š' => "ш",
            'ū' => "у",
            'ž' => "ж",
            
            _ => c.ToString()
        };
    }
}