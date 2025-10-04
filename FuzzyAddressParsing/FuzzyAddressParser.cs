using System.Text.RegularExpressions;

namespace Osmalyzer;

public static class FuzzyAddressParser
{
    /// <summary>
    /// Parse freeform text address into components.
    /// </summary>
    [Pure]
    public static List<FuzzyAddressPart>? TryParseAddress(string raw, params FuzzyAddressHint[] hints)
    {
        if (raw == null) throw new ArgumentNullException(nameof(raw));
        
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        
        string[] splits = raw.Split(',', StringSplitOptions.TrimEntries);

        // Go through splits and try to identify what they are, collect all guesses
        
        List<List<FuzzyAddressPart>> proposedParts = [ ];

        for (int i = 0; i < splits.Length; i++)
        {
            proposedParts.Add([ ]);
            
            string split = splits[i];
            
            if (split == "")
                continue; // todo: if hints given, they return as expected but missing or something?
            
            // Try parse ourselves
            
            List<FuzzyAddressPart>? streetLineResults = TryParseAsStreetLine(split, i);
            if (streetLineResults != null)
                proposedParts[i].AddRange(streetLineResults);
                    
            FuzzyAddressCityPart? cityResult = TryParseAsCity(split, i);
            if (cityResult != null)
                proposedParts[i].Add(cityResult);

            FuzzyAddressPostcodePart? postalCodeResult = TryParseAsPostalCode(split, i);
            if (postalCodeResult != null)
                proposedParts[i].Add(postalCodeResult);
            
            // Do we also have a hint of what we expected?
            // If so, we can gather parsed results and replace them with hinted confidence
            // If we don't have any parsed results, we can add a generic one with hinted confidence
            
            FuzzyAddressHint? hint = hints.FirstOrDefault(h => h.Index == i);
            // todo: authoritative hint, i.e. if hint given, ignore all other guesses and just use hint assumption

            if (hint != null)
            {
                switch (hint)
                {
                    case FuzzyAddressStreetLineHint:
                        FuzzyAddressHouseNamePart? parsedHouseNamePart = proposedParts[i].OfType<FuzzyAddressHouseNamePart>().FirstOrDefault();
                        FuzzyAddressStreetNamePart? parsedStreetNamePart = proposedParts[i].OfType<FuzzyAddressStreetNamePart>().FirstOrDefault();
                        FuzzyAddressStreetNumberPart? parsedStreetNumberPart = proposedParts[i].OfType<FuzzyAddressStreetNumberPart>().FirstOrDefault();
                        
                        if (parsedHouseNamePart != null || parsedStreetNamePart != null || parsedStreetNumberPart != null)
                        {
                            if (parsedHouseNamePart != null)
                            {
                                proposedParts[i].Remove(parsedHouseNamePart);
                                proposedParts[i].Add(parsedHouseNamePart with { Confidence = HintedConfidence(parsedHouseNamePart.Confidence) });
                            }
                            if (parsedStreetNamePart != null)
                            {
                                proposedParts[i].Remove(parsedStreetNamePart);
                                proposedParts[i].Add(parsedStreetNamePart with { Confidence = HintedConfidence(parsedStreetNamePart.Confidence) });
                            }
                            if (parsedStreetNumberPart != null)
                            {
                                proposedParts[i].Remove(parsedStreetNumberPart);
                                proposedParts[i].Add(parsedStreetNumberPart with { Confidence = HintedConfidence(parsedStreetNumberPart.Confidence) });
                            }
                        }
                        else
                        {
                            // Nothing parsed, so we need to assume something, house name is as good as anything here
                            proposedParts[i].Add(new FuzzyAddressHouseNamePart(split, i, FuzzyConfidence.HintedFallback));
                            // todo: generic version?
                        }
                        break;

                    case FuzzyAddressPostcodeHint:
                        FuzzyAddressPostcodePart? parsedPostalPart = proposedParts[i].OfType<FuzzyAddressPostcodePart>().FirstOrDefault();
                        if (parsedPostalPart != null)
                        {
                            proposedParts[i].Remove(parsedPostalPart);
                            proposedParts[i].Add(parsedPostalPart with { Confidence = HintedConfidence(parsedPostalPart.Confidence) });
                        }
                        else
                        {
                            proposedParts[i].Add(new FuzzyAddressPostcodePart(split, i, FuzzyConfidence.HintedFallback));
                        }
                        break;

                    case FuzzyAddressCityHint:
                        FuzzyAddressCityPart? parsedCityPart = proposedParts[i].OfType<FuzzyAddressCityPart>().FirstOrDefault();
                        if (parsedCityPart != null)
                        {
                            proposedParts[i].Remove(parsedCityPart);
                            proposedParts[i].Add(parsedCityPart with { Confidence = HintedConfidence(parsedCityPart.Confidence) });
                        }
                        else
                        {
                            proposedParts[i].Add(new FuzzyAddressCityPart(split, i, FuzzyConfidence.HintedFallback));
                        }
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(hint));
                }
            }
        }
        
        if (proposedParts.All(p => p.Count == 0))
            return null; // nothing parsed at all

        // From the proposed parts, try to select the best ones

        List<FuzzyAddressPart> parts = [ ];
        
        // Gather highest confidence first, then descending remaining
        foreach (FuzzyConfidence minConfidence in Enum.GetValues<FuzzyConfidence>().OrderDescending())
        {
            FuzzyAddressStreetNamePart? selectedStreetName = ExtractBest<FuzzyAddressStreetNamePart>(proposedParts, minConfidence);
            if (selectedStreetName != null)
                parts.Add(selectedStreetName);
            
            FuzzyAddressStreetNumberPart? selectedStreetNumber = ExtractBest<FuzzyAddressStreetNumberPart>(proposedParts, minConfidence);
            if (selectedStreetNumber != null)
                parts.Add(selectedStreetNumber);
            
            FuzzyAddressHouseNamePart? selectedHouseName = ExtractBest<FuzzyAddressHouseNamePart>(proposedParts, minConfidence);
            if (selectedHouseName != null)
                parts.Add(selectedHouseName);
            
            FuzzyAddressCityPart? selectedCity = ExtractBest<FuzzyAddressCityPart>(proposedParts, minConfidence);
            if (selectedCity != null)
                parts.Add(selectedCity);
            
            FuzzyAddressPostcodePart? selectedPostalCode = ExtractBest<FuzzyAddressPostcodePart>(proposedParts, minConfidence);
            if (selectedPostalCode != null)
                parts.Add(selectedPostalCode);
        }

        return parts;
    }


    [MustUseReturnValue]
    private static T? ExtractBest<T>(List<List<FuzzyAddressPart>> proposedParts, FuzzyConfidence minimumConfidence) where T : FuzzyAddressPart
    {
        // Find split, which has the highest confidence for this part
        
        FuzzyConfidence? bestConfidence = null;
        T? bestPart = null;
        int bestPartCount = int.MaxValue; // if confidence equal, prefer split with fewer parts (more certain)

        foreach (List<FuzzyAddressPart> parts in proposedParts)
        {
            T? part = parts
                      .OfType<T>()
                      .Where(p => p.Confidence >= minimumConfidence)
                      .OrderByDescending(p => p.Confidence)
                      .FirstOrDefault();

            if (part != null)
            {
                if (bestConfidence == null || part.Confidence > bestConfidence || 
                    (part.Confidence == bestConfidence && parts.Count < bestPartCount)) // just as confident, but fewer parts is better
                {
                    bestConfidence = part.Confidence;
                    bestPart = part;
                    bestPartCount = parts.Count;
                }
            }
        }

        if (bestPart == null)
            return null;
        
        // Remove this type from all splits, we have selected it now
        
        foreach (List<FuzzyAddressPart> parts in proposedParts)
            parts.RemoveAll(p => p is T);
        
        // Done
        return bestPart;
    }

    [Pure]
    private static List<FuzzyAddressPart>? TryParseAsStreetLine(string split, int index)
    {
        // Name in quotes, e.g. `"Palmas"` 
        if (split.StartsWith('\"') && split.EndsWith('\"'))
            return [ new FuzzyAddressHouseNamePart(split[1..^1], index, FuzzyConfidence.High) ];
        
        (string prefix, string suffix)? splitStreetLine = TrySplitStreetLine(split);
        
        if (splitStreetLine != null)
        {
            string streetName = splitStreetLine.Value.prefix;
            string streetNumber = splitStreetLine.Value.suffix;

            FuzzyAddressStreetNamePart streetNamePart = new FuzzyAddressStreetNamePart(streetName, index, FuzzyConfidence.High);
            FuzzyAddressStreetNumberPart numberPart = new FuzzyAddressStreetNumberPart(streetNumber, index, FuzzyConfidence.High);

            return [ streetNamePart, numberPart ];
        }
        
        return null;
    }

    [Pure]
    private static (string prefix, string suffix)? TrySplitStreetLine(string value)
    {
        // Ends with a number, possibly with letter, possibly with block, preceded by whitespace and something else before
        Match match = Regex.Match(value, @"^(.+?)\s+(\d+[a-zA-Z]?(\s*k-?\d+)?)$");

        if (match.Success)
            return (
                match.Groups[1].Value.Trim(),
                FixNumber(match.Groups[2].Value.Trim())
            );

        return null;
        

        [Pure]
        static string FixNumber(string num)
        {
            // "23" -> "23"
            // "23a" -> "23 A"
            // "23k-1" -> "23 k-1"
            // "23 k1" -> "23 k-1"
            // "23a k1" -> "23A k-1"
            // "23A k1" -> "23A k-1"
            
            Match match = Regex.Match(num, @"^(?<main>\d+)(?<letter>[a-zA-Z]?)(\s*(k-?)?(?<block>\d+))?$");
            
            if (!match.Success)
                return num; // as is
            
            string main = match.Groups["main"].Value;
            string letter = match.Groups["letter"].Value.ToUpperInvariant();
            string block = match.Groups["block"].Value;
            
            if (block != "")
                return main + letter + " k-" + block;
            else
                return main + letter;
        }
    }

    [Pure]
    private static FuzzyAddressCityPart? TryParseAsCity(string split, int index)
    {
        if (KnownFuzzyNames.CityNames.Contains(split))
            return new FuzzyAddressCityPart(split, index, FuzzyConfidence.High);
        
        if (KnownFuzzyNames.LargestTownNames.Contains(split))
            return new FuzzyAddressCityPart(split, index, FuzzyConfidence.High);
        
        if (OnlyLetters(split))
            return new FuzzyAddressCityPart(split, index, FuzzyConfidence.Low); // could be anything
        
        // todo: check words
        
        return null;
    }

    [Pure]
    private static FuzzyAddressPostcodePart? TryParseAsPostalCode(string split, int index)
    {
        string cleaned = split.ToUpperInvariant();
        
        cleaned = split.Replace(" ", ""); // remove all spaces by default
        
        cleaned = split.Replace("–", "-"); // n-dash
        cleaned = split.Replace("—", "-"); // m-dash
        
        cleaned = cleaned.Replace("LV ", "LV-"); // restore dash for specifically prefix
        
        if (Regex.IsMatch(cleaned, @"^LV-\d{4}$"))
            return new FuzzyAddressPostcodePart(cleaned, index, FuzzyConfidence.High);
        
        if (Regex.IsMatch(cleaned, @"^\d{4}$"))
            return new FuzzyAddressPostcodePart("LV-" + cleaned, index, FuzzyConfidence.Low);
        
        return null;
    }

    [Pure]
    private static bool OnlyLetters(string value)
    {
        return Regex.IsMatch(value, @"^[\p{L} ]+$");
    }

    [Pure]
    private static FuzzyConfidence HintedConfidence(FuzzyConfidence confidence)
    {
        switch (confidence)
        {
            case FuzzyConfidence.High:
                return FuzzyConfidence.HintedHigh;
            
            case FuzzyConfidence.Low:
                return FuzzyConfidence.HintedLow;

            case FuzzyConfidence.HintedFallback:
            case FuzzyConfidence.HintedLow:
            case FuzzyConfidence.HintedHigh:
                throw new InvalidOperationException();
            
            default:
                throw new ArgumentOutOfRangeException(nameof(confidence), confidence, null);
        }
    }
}


public abstract record FuzzyAddressPart(string Value, int Index, FuzzyConfidence Confidence);
public record FuzzyAddressStreetNamePart(string Value, int Index, FuzzyConfidence Confidence) : FuzzyAddressPart(Value, Index, Confidence);
public record FuzzyAddressStreetNumberPart(string Value, int Index, FuzzyConfidence Confidence) : FuzzyAddressPart(Value, Index, Confidence);
public record FuzzyAddressHouseNamePart(string Value, int Index, FuzzyConfidence Confidence) : FuzzyAddressPart(Value, Index, Confidence);
public record FuzzyAddressCityPart(string Value, int Index, FuzzyConfidence Confidence) : FuzzyAddressPart(Value, Index, Confidence);
public record FuzzyAddressPostcodePart(string Value, int Index, FuzzyConfidence Confidence) : FuzzyAddressPart(Value, Index, Confidence);


public enum FuzzyConfidence
{
    /// <summary> We are not sure </summary>
    Low = 0,

    /// <summary> We are at least fairly sure </summary>
    High = 1,

    /// <summary> We were told what this is, but we couldn't parse it at all </summary>
    HintedFallback = 2,
    
    /// <summary> We were told what this is, but we are not very confident it's true, although we did parse it </summary>
    HintedLow = 3,
    
    /// <summary> We were told what this is, and we are confident it's true </summary>
    HintedHigh = 4
}


public abstract record FuzzyAddressHint(int Index);
public record FuzzyAddressStreetLineHint(int Index) : FuzzyAddressHint(Index);
public record FuzzyAddressCityHint(int Index) : FuzzyAddressHint(Index);
public record FuzzyAddressPostcodeHint(int Index) : FuzzyAddressHint(Index);
