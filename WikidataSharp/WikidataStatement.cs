using JetBrains.Annotations;

namespace WikidataSharp;

public class WikidataStatement
{
    [PublicAPI]
    public long PropertyID { get; }
    
    [PublicAPI]
    public string Value { get; }

    
    public WikidataStatement(long propertyID, string value)
    {
        PropertyID = propertyID;
        Value = value;
    }
}