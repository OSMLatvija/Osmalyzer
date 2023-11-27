public class WaitForTime : BrowsingAction
{
    public int Milliseconds { get; }

    
    public WaitForTime(int milliseconds)
    {
        Milliseconds = milliseconds;
    }
}