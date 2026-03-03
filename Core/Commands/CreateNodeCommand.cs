namespace Osmalyzer.Commands;

internal class CreateNodeCommand : Command
{
    public long? Id { get; }
    
    public OsmCoord Coord { get; }
    
    public OsmNode? CreatedNode { get; private set; }

    
    internal CreateNodeCommand(OsmData data, OsmCoord coord)
        : base(data)
    {
        Coord = coord;
        Id = null;
    }
    
    internal CreateNodeCommand(OsmData data, long id, OsmCoord coord)
        : base(data)
    {
        Coord = coord;
        Id = id;
    }
    
    
    internal override Command Apply()
    {
        // Actuate

        OsmNode newNode;
        
        if (Id == null)
            newNode = new OsmNode(Coord, Data);
        else
            newNode = new OsmNode(Id.Value, Coord, Data);
        
        Data.RegisterElement(newNode);
        
        // Store created node for reference
        CreatedNode = newNode;
        
        // Return inverse command, i.e. delete
        return new DeleteNodeCommand(Data, newNode);
    }
}