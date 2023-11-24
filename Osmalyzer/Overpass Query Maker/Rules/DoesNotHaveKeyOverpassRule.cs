namespace Osmalyzer;

public class DoesNotHaveKeyOverpassRule : OverpassRule
{
    public string Key { get; }

    public DoesNotHaveKeyOverpassRule(string key)
    {
        Key = key;
    }
}