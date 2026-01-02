using JetBrains.Annotations;

namespace WikidataSharp;

public class WikidataStatement
{
    [PublicAPI]
    public long PropertyID { get; }
    
    [PublicAPI]
    public string Value { get; }

    [PublicAPI]
    public string DataType { get; }

    
    public WikidataStatement(long propertyID, string value, string dataType)
    {
        PropertyID = propertyID;
        Value = value;
        DataType = dataType;
    }


    public override string ToString()
    {
        return $"P{PropertyID} [{DataType}] = {Value}";
    }
}