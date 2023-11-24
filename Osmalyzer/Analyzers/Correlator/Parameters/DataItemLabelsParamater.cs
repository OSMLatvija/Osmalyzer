namespace Osmalyzer;

public class DataItemLabelsParamater : CorrelatorParamater
{
    public string LabelSingular { get; }
    
    public string LabelPlural { get; }

    
    public DataItemLabelsParamater(string labelSingular, string labelPlural)
    {
        LabelSingular = labelSingular;
        LabelPlural = labelPlural;
    }
}