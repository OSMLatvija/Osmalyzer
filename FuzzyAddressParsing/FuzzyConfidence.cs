namespace Osmalyzer;

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