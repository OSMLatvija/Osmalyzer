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
        name = ReplaceWithPreserveCase(name, "pju", "пю");

        // Гипократа not Хипократа for Hipokrāta
        name = ReplaceWithPreserveCase(name, "hi", "ги");
        
        // Кришьяня not Кришяня for Krišjāņa
        name = ReplaceWithPreserveCase(name, "šja", "шья");
        name = ReplaceWithPreserveCase(name, "šjā", "шья");
        
        // Generic character to character conversion
        
        string translit = "";
        
        foreach (char c in name)
        {
            string newC = TranslitLatinToCyrillicChar(c);

            translit += newC;
        }

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