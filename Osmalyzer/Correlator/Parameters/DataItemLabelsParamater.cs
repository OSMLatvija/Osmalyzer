namespace Osmalyzer;

public class DataItemLabelsParamater : CorrelatorParamater
{
    public string LabelSingular { get; }
    
    public string LabelPlural { get; }

    
    /// <summary>
    /// 
    /// Sentence capitalization (of first letter) is not needed. 
    /// </summary>
    public DataItemLabelsParamater(string labelSingular, string labelPlural)
    {
        LabelSingular = labelSingular;
        LabelPlural = labelPlural;
    }
}