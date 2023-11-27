public class ClickElementOfClass : BrowsingAction
{
    public string ClassName { get; }
    
    public int Index { get; }


    public ClickElementOfClass(string className, int index = 0)
    {
        ClassName = className;
        Index = index;
    }
}