using System.Text.RegularExpressions;

namespace Osmalyzer;

public static class FuzzyAddressParser
{
    /// <summary>
    /// Parse freeform text address into components.
    /// </summary>
    [Pure]
    public static FuzzyAddress? TryParseAddress(string raw, params FuzzyAddressHint[] hints)
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
            
            FuzzyAddressPart[]? streetLineResult = TryParseAsStreetLine(split, i);
            if (streetLineResult != null)
                proposedParts[i].AddRange(streetLineResult);

            FuzzyAddressMunicipalityPart? municipalityResult = TryParseAsMunicipality(split, i);
            if (municipalityResult != null)
                proposedParts[i].Add(municipalityResult);

            FuzzyAddressParishPart? parishResult = TryParseAsParish(split, i);
            if (parishResult != null)
                proposedParts[i].Add(parishResult);
                    
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

            if (hint != null)
            {
                switch (hint)
                {
                    case FuzzyAddressStreetLineHint:
                        FuzzyAddressHouseNamePart? parsedHouseNamePart = proposedParts[i].OfType<FuzzyAddressHouseNamePart>().FirstOrDefault();
                        FuzzyAddressStreetNameAndNumberPart? parsedStreetNamePart = proposedParts[i].OfType<FuzzyAddressStreetNameAndNumberPart>().FirstOrDefault();
                        
                        if (parsedHouseNamePart != null || parsedStreetNamePart != null)
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

                    case FuzzyAddressParishHint:
                        FuzzyAddressParishPart? parsedParishPart = proposedParts[i].OfType<FuzzyAddressParishPart>().FirstOrDefault();
                        if (parsedParishPart != null)
                        {
                            proposedParts[i].Remove(parsedParishPart);
                            proposedParts[i].Add(parsedParishPart with { Confidence = HintedConfidence(parsedParishPart.Confidence) });
                        }
                        else
                        {
                            proposedParts[i].Add(new FuzzyAddressParishPart(split, i, FuzzyConfidence.HintedFallback));
                        }
                        break;

                    case FuzzyAddressMunicipalityHint:
                        FuzzyAddressMunicipalityPart? parsedMunicipalityPart = proposedParts[i].OfType<FuzzyAddressMunicipalityPart>().FirstOrDefault();
                        if (parsedMunicipalityPart != null)
                        {
                            proposedParts[i].Remove(parsedMunicipalityPart);
                            proposedParts[i].Add(parsedMunicipalityPart with { Confidence = HintedConfidence(parsedMunicipalityPart.Confidence) });
                        }
                        else
                        {
                            proposedParts[i].Add(new FuzzyAddressMunicipalityPart(split, i, FuzzyConfidence.HintedFallback));
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
            FuzzyAddressHouseNamePart[]? selectedHouseName = ExtractBest<FuzzyAddressHouseNamePart>(proposedParts, minConfidence);
            if (selectedHouseName != null)
                parts.AddRange(selectedHouseName);
            
            FuzzyAddressStreetNameAndNumberPart[]? selectedStreetNameAndNumber = ExtractBest<FuzzyAddressStreetNameAndNumberPart>(proposedParts, minConfidence);
            if (selectedStreetNameAndNumber != null)
                parts.AddRange(selectedStreetNameAndNumber);
            
            FuzzyAddressCityPart[]? selectedCity = ExtractBest<FuzzyAddressCityPart>(proposedParts, minConfidence);
            if (selectedCity != null)
                parts.AddRange(selectedCity);

            FuzzyAddressParishPart[]? selectedParish = ExtractBest<FuzzyAddressParishPart>(proposedParts, minConfidence);
            if (selectedParish != null)
                parts.AddRange(selectedParish);

            FuzzyAddressMunicipalityPart[]? selectedMunicipality = ExtractBest<FuzzyAddressMunicipalityPart>(proposedParts, minConfidence);
            if (selectedMunicipality != null)
                parts.AddRange(selectedMunicipality);
            
            FuzzyAddressPostcodePart[]? selectedPostalCode = ExtractBest<FuzzyAddressPostcodePart>(proposedParts, minConfidence);
            if (selectedPostalCode != null)
                parts.AddRange(selectedPostalCode);
        }

        return new FuzzyAddress(parts);
    }


    [MustUseReturnValue]
    private static T[]? ExtractBest<T>(List<List<FuzzyAddressPart>> proposedParts, FuzzyConfidence minimumConfidence) where T : FuzzyAddressPart
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
        
        // Clear the split as its part has been assigned, and we can't have more from here
        
        proposedParts[bestPart.Index].Clear();
        
        // Do have a closely-linked alternative that we exclude by getting selected?
        // e.g. if we select street name+number, we should not also select house name -- these are mutually exclusive
        
        if (bestPart is FuzzyAddressStreetNameAndNumberPart)
        {
            // Remove any house names from all splits, they are mutually exclusive with street name+number
            foreach (List<FuzzyAddressPart> parts in proposedParts)
                parts.RemoveAll(p => p is FuzzyAddressHouseNamePart);
        }
        else if (bestPart is FuzzyAddressHouseNamePart)
        {
            // Remove any street name+number from all splits, they are mutually exclusive with house name
            foreach (List<FuzzyAddressPart> parts in proposedParts)
                parts.RemoveAll(p => p is FuzzyAddressStreetNameAndNumberPart);
        }
        
        if (bestPart.Siblings != null)
        {
            List<T> results = [ bestPart ];
            
            // Remove siblings that don't match our type
            // e.g. "Kalnu iela 12 / Indrāni" might match street name+number and house name,
            // but it's a high confidence street name+number and low confidence house name,
            // so when we select street name+number, we should also remove the house name sibling
            // At the same time, we want to keep any siblings of the same type,
            // e.g. "Kalnu iela 12 / Leju iela 13"
            
            foreach (FuzzyAddressPart sibling in bestPart.Siblings)
                if (sibling is T matchingSibling)
                    results.Add(matchingSibling);
                else
                    proposedParts[sibling.Index].Remove(sibling);

            return results.ToArray();
        }

        // Done
        return [ bestPart ];
    }

    [Pure]
    private static FuzzyAddressPart[]? TryParseAsStreetLine(string split, int index)
    {
        if (LooksLikePotentialParishOrMunicipality(split))
            return null; // avoid false positives

        FuzzyAddressHouseNamePart? addressHouseNamePart = TryParseAsHouseName(split, index);
        
        // Try to split into two delimited street line address parts

        FuzzyAddressPart[]? streetNameAndNumber = TryParseAsStreetNameAndNumber(split, index);

        // Note that both house name and street name+number are possible,
        // e.g. "Palmas 5" - is it "Palmas iela 5" or house name "Palmas 5"?
        
        // Results
        
        if (addressHouseNamePart == null && streetNameAndNumber == null)
            return null; // nothing parsed

        List<FuzzyAddressPart> results = [ ];
        
        if (addressHouseNamePart != null)
            results.Add(addressHouseNamePart);
        
        if (streetNameAndNumber != null)
            results.AddRange(streetNameAndNumber);

        // If both interpretations exist and are equally confident (low/high), mark them as fallbacks to each other
        if (addressHouseNamePart != null && streetNameAndNumber != null && streetNameAndNumber.Length == 1)
        {
            if (streetNameAndNumber[0] is FuzzyAddressStreetNameAndNumberPart streetPart)
            {
                if (addressHouseNamePart.Confidence == streetPart.Confidence) // todo: hinted same?
                {
                    addressHouseNamePart.AddFallback(streetPart);
                    streetPart.AddFallback(addressHouseNamePart);
                }
            }
        }

        return results.ToArray();
    }

    private static FuzzyAddressPart[]? TryParseAsStreetNameAndNumber(string split, int index)
    {
        if (split.Contains('/'))
        {
            string[] slashParts = split.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (slashParts.Length == 2)
            {
                // todo: try all permutations with move parts in case we have a number like "3/5"?
                
                FuzzyAddressPart[]? leftPart = TryParseAsStreetLine(slashParts[0], index);
                FuzzyAddressPart[]? rightPart = TryParseAsStreetLine(slashParts[1], index);

                if (leftPart != null && rightPart != null)
                {
                    foreach (FuzzyAddressPart left in leftPart)
                    {
                        foreach (FuzzyAddressPart right in rightPart)
                        {
                            left.AddSibling(right);
                            right.AddSibling(left);
                        }
                    }
                    
                    return leftPart.Concat(rightPart).ToArray();
                }
            }
        }
        
        // Try split into street name and number
        
        (string street, string number, string? letter, string? unit, string? block, FuzzyConfidence confidence)? splitStreetLine 
            = TrySplitStreetLine(split);
        
        if (splitStreetLine != null)
        {
            string[] parts = splitStreetLine.Value.street.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            bool anyPartIsWord = parts.Any(p => p.Length >= 3);
            // Otherwise, we detect stuff like "LV 1234" as street name, presumably we don't get shorter than "Īsā 3" or something
            
            if (anyPartIsWord)
            {
                string streetName = splitStreetLine.Value.street;
                string streetNumber = FixNumber(splitStreetLine.Value.number, splitStreetLine.Value.letter, splitStreetLine.Value.block);
                string? unit = splitStreetLine.Value.unit;
                FuzzyConfidence confidence = splitStreetLine.Value.confidence;

                return [ new FuzzyAddressStreetNameAndNumberPart(streetName, streetNumber, unit, index, confidence) ];
            }
        }
        
        return null;
    }

    [Pure]
    private static FuzzyAddressHouseNamePart? TryParseAsHouseName(string value, int index)
    {
        if (LooksLikePotentialParishOrMunicipality(value))
            return null; // avoid false positives
        
        if (LooksLikeStreetName(value)) // e.g. "Xxx iela" or whatever
            return null; // avoid false positives
        // Note that we can have numbers in house names, so we can't auto-exclude them
        
        value = value.Replace("“", "\"").Replace("”", "\"")
                     .Replace("‘", "'").Replace("’", "'")
                     .Trim();
        
        bool inQuotes = value.StartsWith('"') && value.EndsWith('"');
        
        // Name in quotes, e.g. `"Palmas"` 
        if (inQuotes)
            value = value[1..^1].Trim();
            
        if (value.Length < 3)
            return null; // totally too short to be a name
            
        int numberOfLetters = value.Count(char.IsLetter);
        if (numberOfLetters < 3)
            return null; // too few letters to be a name
            
        return new FuzzyAddressHouseNamePart(value, index, inQuotes ? FuzzyConfidence.High : FuzzyConfidence.Low);
    }

    [Pure]
    private static (string street, string number, string? letter, string? unit, string? block, FuzzyConfidence confidence)? TrySplitStreetLine(string value)
    {
        // Name + main number + optional letter (but avoid treating block 'k' as letter when followed by digits), optional unit (e.g., -3), optional block (k-24 or k24)
        // "Krānu iela 35"
        // "Krānu iela 35A"
        // "Krānu iela 35-3"
        // "Krānu iela 35A-3"
        // "Krānu iela 35 k-2"
        // "Krānu iela 35A k-2"
        // "Krānu iela 35 k2"
        // "Krānu iela 35/37"
        Match match = Regex.Match(
            value,
            @"^(.+?)\s+(?<number>\d+(?:\/\d+)?)(?:\s*(?!(?:k\s*-?\d))(?<letter>[a-zA-Z]))?(?:\s*-(?<unit>\d+))?(?:\s*k-?(?<block>\d+))?$",
            RegexOptions.IgnoreCase
        );

        if (!match.Success)
            return null;

        string name = match.Groups[1].Value.Trim();
        
        if (name.Length < 3)
            return null; // totally too short to be a name
            
        int numberOfLetters = name.Count(char.IsLetter);
        if (numberOfLetters < 3)
            return null; // too few letters to be a name
        
        string fixedName = FixName(name, out bool hadExpectedSuffix);
        string number = match.Groups["number"].Value.Trim();
        string? letter = match.Groups["letter"].Success ? match.Groups["letter"].Value.Trim() : null;
        string? unit = match.Groups["unit"].Success ? match.Groups["unit"].Value.Trim() : null;
        string? block = match.Groups["block"].Success ? match.Groups["block"].Value.Trim() : null;

        return (
            fixedName, 
            number, letter, unit, block, 
            hadExpectedSuffix ? FuzzyConfidence.High : FuzzyConfidence.Low
        );

        [Pure]
        static string FixName(string name, out bool hadExpectedSuffix)
        {
            // "Krānu ielā" -> "Krānu iela"
            // "Krānu" -> "Krānu iela"

            hadExpectedSuffix = false;
            
            foreach (KnownFuzzyNames.StreetNameSuffix suffix in KnownFuzzyNames.StreetNameSuffixes)
            {
                // "ielā" -> "iela" etc.
                if (name.EndsWith(suffix.Locative, StringComparison.InvariantCultureIgnoreCase))
                {    
                    name = name[..^suffix.Locative.Length] + suffix.Nominative;
                    hadExpectedSuffix = true;
                    break;
                }
                else if (name.EndsWith(suffix.Nominative, StringComparison.InvariantCultureIgnoreCase))
                {
                    hadExpectedSuffix = true;
                    break;
                }
            }

            if (!hadExpectedSuffix)
            {
                // No known suffix, so just assume and add "iela" as the most common one
                // todo: don't literally add, let address match try to add any known suffix?
                name += " " + KnownFuzzyNames.StreetNameSuffixes.First().Nominative;
            }
            
            return name;
        }
    }

    [Pure]
    private static string FixNumber(string main, string? letter, string? block)
    {
        string core = main + (string.IsNullOrWhiteSpace(letter) ? "" : letter!.Trim().ToUpperInvariant());
        
        if (!string.IsNullOrWhiteSpace(block))
            return core + " k-" + block!.Trim();
        
        return core;
    }

    [Pure]
    private static FuzzyAddressCityPart? TryParseAsCity(string split, int index)
    {
        if (LooksLikePotentialParishOrMunicipality(split))
            return null; // avoid false positives
        
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
    private static FuzzyAddressMunicipalityPart? TryParseAsMunicipality(string split, int index)
    {
        // todo: keep a full list?
        
        Match match = Regex.Match(split, @"^(?<name>.+?)\s+nov(?:\.|ads?)$", RegexOptions.IgnoreCase);
        if (!match.Success)
            return null;

        string name = match.Groups["name"].Value.Trim();
        if (name.Length < 4) // nothing shorter than "Cēsu novads"
            return null;
        
        if (KnownFuzzyNames.MunicipalityNames.TryGetValue(name, out string? baseValue))
            return new FuzzyAddressMunicipalityPart(baseValue + " novads", index, FuzzyConfidence.High);
        
        if (name.Any(char.IsDigit))
            return null; // can't have digits in municipality name

        string normalized = name + " novads";
        return new FuzzyAddressMunicipalityPart(normalized, index, FuzzyConfidence.Low);
    }

    [Pure]
    private static FuzzyAddressParishPart? TryParseAsParish(string split, int index)
    {
        Match match = Regex.Match(split, @"^(?<name>.+?)\s+pag(?:\.|asts?)$", RegexOptions.IgnoreCase);
        if (!match.Success)
            return null;

        string name = match.Groups["name"].Value.Trim();
        if (name.Length < 4) // nothing shorter than "Apes pagasts"
            return null;
        
        if (KnownFuzzyNames.ParishNames.TryGetValue(name, out string? baseValue))
            return new FuzzyAddressParishPart(baseValue + " pagasts", index, FuzzyConfidence.High);
        
        if (name.Any(char.IsDigit))
            return null; // can't have digits in parish name

        string normalized = name + " pagasts";
        return new FuzzyAddressParishPart(normalized, index, FuzzyConfidence.Low);
    }

    [Pure]
    private static bool LooksLikePotentialParishOrMunicipality(string value)
    {
        value = value.ToLowerInvariant();

        return
            value.EndsWith("pagasts") ||
            value.EndsWith("pag.") ||
            value == "pagasts" || // probably broken data and some delimiter issues
            value.EndsWith("novads") ||
            value.EndsWith("nov.") ||
            value == "novads"; // probably broken data and some delimiter issues
    }

    [Pure]
    private static bool LooksLikeStreetName(string name)
    {
        foreach (KnownFuzzyNames.StreetNameSuffix suffix in KnownFuzzyNames.StreetNameSuffixes)
        {
            if (name.EndsWith(suffix.Locative, StringComparison.InvariantCultureIgnoreCase) ||
                name.EndsWith(suffix.Nominative, StringComparison.InvariantCultureIgnoreCase))
                return true;
        }

        return false;
    }

    [Pure]
    private static FuzzyAddressPostcodePart? TryParseAsPostalCode(string split, int index)
    {
        if (Regex.IsMatch(split, @"^LV-\d{4}$")) // perfect match
            return new FuzzyAddressPostcodePart(split, index, FuzzyConfidence.High);

        string cleaned = split.ToUpperInvariant()
                              .Replace("LV ", "LV-") // make sure we have dash
                              .Replace(" ", "") // remove all other spaces
                              .Replace("–", "-") // n-dash
                              .Replace("—", "-"); // m-dash
        
        if (Regex.IsMatch(cleaned, @"^LV-\d{4}$")) // "LV-1234" after cleaning
            return new FuzzyAddressPostcodePart(cleaned, index, FuzzyConfidence.High);

        if (Regex.IsMatch(cleaned, @"^LV\d{4}$")) // "LV1234" - never had a dash or space
            return new FuzzyAddressPostcodePart(cleaned.Replace("LV", "LV-"), index, FuzzyConfidence.High);
        
        if (Regex.IsMatch(cleaned, @"^\d{4}$")) // "1234" - never had LV
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