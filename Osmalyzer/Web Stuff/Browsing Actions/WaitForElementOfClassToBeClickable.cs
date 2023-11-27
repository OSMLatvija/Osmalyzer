public class WaitForElementOfClassToBeClickable : BrowsingAction
{
    public string ClassName { get; }

    
    public WaitForElementOfClassToBeClickable(string className)
    {
        ClassName = className;
    }
}