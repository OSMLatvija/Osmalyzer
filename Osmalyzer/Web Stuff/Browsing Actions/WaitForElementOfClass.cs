public class WaitForElementOfClass : BrowsingAction
{
    public string ClassName { get; }

    
    public WaitForElementOfClass(string className)
    {
        ClassName = className;
    }
}