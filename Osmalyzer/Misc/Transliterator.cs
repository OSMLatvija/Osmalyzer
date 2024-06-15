using System;
using System.Text.RegularExpressions;

namespace Osmalyzer;

public static class Transliterator
{
    [Pure]
    public static string TransliterateFromLvToRu(string name)
    {
        // Special cases pre-process
        
        // Numbers don't have period
        name = Regex.Replace(name, @"(\d+).", "$1");

        // Replace soft consonant followed by another consonant with soft sign
        name = Regex.Replace(name, @"ņ(?![euioaēūīōāņ])", "нь");
        name = Regex.Replace(name, @"ķ(?![euioaēūīōāķ])", "кь");
        name = Regex.Replace(name, @"ļ(?![euioaēūīōāļ])", "ль");
        name = Regex.Replace(name, @"ģ(?![euioaēūīōāģ])", "гь");

        // For combinations consonant + j + vowel add a soft sign into transliteration, to emphasize presence of sound `j`
        // Example: Канепью not Канепю for Kaņepju
        name = Regex.Replace(name, @"(?<=[rtplkgfdscvbnmļķņčģ])(?=j[aeuioāēūīō])", "ь", RegexOptions.IgnoreCase);
        
        // Эйзенштейна not Эизенштейна for Eizenšteina - but only at the start of word
        name = Regex.Replace(name, @"\b[EĒ]i", "Эй");
        name = Regex.Replace(name, @"\b[eē]i", "эй");
        
        // Элизабетес not Елизабетес for Elizabetes - but only at the start of word
        name = Regex.Replace(name, @"\b[EĒ]", "Э");
        name = Regex.Replace(name, @"\b[eē]", "э");

        // Мейстару not Меистару for Meistaru
        name = ReplaceWithPreserveCase(name, "ai", "ай");
        name = ReplaceWithPreserveCase(name, "ei", "ей");
        name = ReplaceWithPreserveCase(name, "ui", "уй");

        // Тиргоню not Тиргону for Tirgoņu
        //name = ReplaceWithPreserveCase(name, "ču", "чю");
        name = ReplaceWithPreserveCase(name, "ģu", "гю");
        name = ReplaceWithPreserveCase(name, "ķu", "кю");
        name = ReplaceWithPreserveCase(name, "ļu", "лю");
        name = ReplaceWithPreserveCase(name, "ņu", "ню");
        //name = ReplaceWithPreserveCase(name, "šu", "шю");
        //name = ReplaceWithPreserveCase(name, "žu", "жю");
        
        // Екабпилс not Йекабпилс for Jēkabpils
        name = ReplaceWithPreserveCase(name, "je", "е");
        name = ReplaceWithPreserveCase(name, "jē", "е");

        // Кришьяня not Кришяня for Krišjāņa
        name = ReplaceWithPreserveCase(name, "šja", "шья");
        name = ReplaceWithPreserveCase(name, "šjā", "шья");
        
        // Стацияс not Стацийас for Stacijas
        name = ReplaceWithPreserveCase(name, "ja", "я");
        name = ReplaceWithPreserveCase(name, "jā", "я");
        
        // Кляву not Клаву for Kļavu
        name = ReplaceWithPreserveCase(name, "ļa", "ля");
        name = ReplaceWithPreserveCase(name, "ļā", "ля");

        // Илменя not Илмена for Ilmeņa
        name = ReplaceWithPreserveCase(name, "ņa", "ня");

        // Гравю not Гравйу for Grāvju
        name = ReplaceWithPreserveCase(name, "ju", "ю");
        name = ReplaceWithPreserveCase(name, "jū", "ю");

        // Гипократа not Хипократа for Hipokrāta
        name = ReplaceWithPreserveCase(name, "hi", "ги");
        
        // Generic character to character conversion
        
        string translit = "";
        
        foreach (char c in name)
        {
            string newC = TranslitLatinToCyrillicChar(c);

            translit += newC;
        }

        // Post processing 
        translit = translit.Replace("ьйо","ё");

        return translit;
    }

    
    [Pure]
    public static string TransliterateFromLvToEn(string name)
    {
        string translit = name;
        translit = Regex.Replace(translit, @"(?<!1)1\.\s*$", @"1st");
        translit = Regex.Replace(translit, @"(?<!1)2\.\s*$", @"2nd");
        translit = Regex.Replace(translit, @"(?<!1)3\.\s*$", @"3rd");
        translit = Regex.Replace(translit, @"(\d)\.\s*$", @"$1th");
        return translit;
    }

    private static string ReplaceWithPreserveCase(string str, string find, string replace)
    {
        string lowerFind = find.ToLower();
        
        do
        {
            int index = str.ToLower().IndexOf(lowerFind, StringComparison.Ordinal);
            
            if (index == -1)
                return str;

            str =
                str[..index] +
                SwapCase(str[index..(index + find.Length)], replace) +
                str[(index + find.Length)..];

        } while (true);


        static string SwapCase(string from, string to)
        {
            // Exact length - exact case "swap"
            
            string res = "";

            for (int i = 0; i < to.Length; i++)
            {
                char c = to[i];

                res += i <= from.Length - 1 && char.IsUpper(from[i]) ? char.ToUpper(c) : char.ToLower(c);
            }

            return res;
        }
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