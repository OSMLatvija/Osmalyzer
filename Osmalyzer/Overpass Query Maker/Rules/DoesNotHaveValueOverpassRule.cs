namespace Osmalyzer
{
    public class DoesNotHaveValueOverpassRule : OverpassRule
    {
        public string Key { get; }
        public string Value { get; }

        public DoesNotHaveValueOverpassRule(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}