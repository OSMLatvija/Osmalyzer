namespace Osmalyzer;

public class HasValueOverpassRule : OverpassRule
{
    public string Key { get; }
    public string Value { get; }

    public HasValueOverpassRule(string key, string value)
    {
        Key = key;
        Value = value;
    }
}