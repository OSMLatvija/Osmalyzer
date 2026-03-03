namespace Osmalyzer.Commands;

internal abstract class Command
{
    public OsmData Data { get; }

    
    protected Command(OsmData data)
    {
        Data = data;
    }

    
    [MustUseReturnValue]
    internal abstract Command? Apply();
}