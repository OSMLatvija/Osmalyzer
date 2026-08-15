namespace Osmalyzer;

public static class PublicTransportHelpers
{
    private static readonly Dictionary<string, string> _cleanedStopNameCache = [ ];
    
    
    [Pure]
    public static bool IsStopNameMatchGoodEnough(string ptStopName, string osmStopName)
    {
        // Quick check first, may be we don't need to do anything
        if (ptStopName == osmStopName)
            return true;

        if (_cleanedStopNameCache.TryGetValue(ptStopName, out string? cleanedPtStopName))
        {
            // We already cleaned this name, so use it
            ptStopName = cleanedPtStopName;
        }
        else
        {
            // Clean the name and store it for later
            cleanedPtStopName = CleanName(ptStopName);
            _cleanedStopNameCache[ptStopName] = cleanedPtStopName;
            ptStopName = cleanedPtStopName;
        }

        if (_cleanedStopNameCache.TryGetValue(osmStopName, out string? cleanedOsmStopName))
        {
            // We already cleaned this name, so use it
            osmStopName = cleanedOsmStopName;
        }
        else
        {
            // Clean the name and store it for later
            cleanedOsmStopName = CleanName(osmStopName);
            _cleanedStopNameCache[osmStopName] = cleanedOsmStopName;
            osmStopName = cleanedOsmStopName;
        }
        

        [Pure]
        static string CleanName(string name)
        {
            // Stops never really differ by capitalization, so just lower them and avoid weird capitalization in addition to everything else
            // Rezekne "18.Novembra iela" vs OSM "18. novembra iela"
            name = name.ToLower();

            // Remove erroneous spaces
            // e.g. ATD "DS  Salūts"
            name = Regex.Replace(name, @"\s{2,}", @" ");
            
            // Trim parenthesis from OSM
            // Jurmalas OSM stops have a lot of parenthesis, like JS "Majoru stacija" vs OSM "Majoru stacija (Majori)"
            name = Regex.Replace(name, @" \([^\(\)]+\)$", @"");
            
            // Trim brackets from GTFS
            // A couple Jurmalas GTFS stops have brackets, like JS "Promenādes iela [Promenādes iela]" and "Promenādes iela [Rīgas iela]" vs OSM "Promenādes iela" and "Promenādes iela"
            name = Regex.Replace(name, @" \[[^\[\]]+\]$", @"");

            // Sometimes proper quotes are inconsistent between the two
            // OSM "Arēna "Rīga"" vs RS "Arēna Rīga" or OSM ""Bērnu pasaule"" vs RS "Bērnu pasaule"
            // or opposite OSM "Dzintars" vs RS ""Dzintars""
            name = name.Replace("\"", "");
            
            // Both OSM and RS stops are inconsistent about spacing around characters
            // "2.trolejbusu parks" or "Jaunciema 2.šķērslīnija" also all the abbreviated "P.Lejiņa iela"
            // "Upesgrīvas iela/ Spice"
            // OSM "TEC-2 pārvalde" vs RS "TEC- 2 pārvalde" or OSM "Preču-2" vs RS "Preču - 2"
            name = Regex.Replace(name, @"(?<! )([\./-])", @" $1");
            name = Regex.Replace(name, @"([\./-])(?! )", @"$1 ");

            // Abbreviated street designation "14.novembra bulv." in LST
            name = name.Replace("bulv.", "bulvāris");
            
            // "Pagr.uz slimnīcu" abbreviated from "Pagrieziens uz slimnīcu" in LST
            name = name.Replace("Pagr.uz", "Pagrieziens uz");
            
            return name;
        }
        

        // Are cleaned names equal?
        if (ptStopName == osmStopName)
            return true;
        
        
        // Special checks
        
        // RS likes to abbreviate names for stops while OSM spells them out
        // OSM "Eduarda Smiļģa iela" vs RS "E.Smiļģa iela"
        // Because there are so many like this, I will consider them correct for now, even if they aren't technically accurate 
        if (ptStopName.Contains('.') && !osmStopName.Contains('.'))
        {
            string[] ptSplit = ptStopName.Split('.');
            if (ptSplit.Length == 2)
            {
                string ptPrefix = ptSplit[0].TrimEnd(); // "E"
                string ptSuffiix = ptSplit[1].TrimStart(); // "Smiļģa iela"
    
                if (osmStopName.StartsWith(ptPrefix) && osmStopName.EndsWith(ptSuffiix)) // not a perfect check, but good enough
                    return true;
            }
        }
            
        // RS also has some double names for some reason when OSM and real-life has just one "part"
        // RS "Botāniskais dārzs/Rīgas Stradiņa universitāte" vs OSM "Botāniskais dārzs
        if (ptStopName.Contains('/'))
        {
            string[] ptSplit = ptStopName.Split('/');
            if (ptSplit.Length == 2)
            {
                string ptFirst = ptSplit[0].TrimEnd(); // "Botāniskais dārzs"
                string ptSecond = ptSplit[1].TrimStart(); // "Rīgas Stradiņa universitāte"
    
                if (osmStopName == ptFirst || osmStopName == ptSecond)
                    return true;
            }
        }
            
        // Couldn't match anything
        return false;
    }
}