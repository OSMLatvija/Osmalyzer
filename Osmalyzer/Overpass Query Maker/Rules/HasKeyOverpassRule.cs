namespace Osmalyzer;

public class HasKeyOverpassRule : OverpassRule
{
    public string Key { get; }

    public HasKeyOverpassRule(string key)
    {
        Key = key;
    }
}