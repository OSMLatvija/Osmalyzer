namespace Osmalyzer;

public class LatviaPostItem : ParcelLocker
{
    public LatviaPostItemType? ItemType { get; }
    
    
    public LatviaPostItem(string? name, string? address, string? code, LatviaPostItemType itemType, OsmCoord coord)
        : base("Latvijas Pasts", code, name, address, coord)
    {
        ItemType = itemType;
    }
    
    
    public override string ReportString()
    {
        return
            Operator + ItemType +
            (Name != null ? " `" + Name + "`" : "") +
            (Id != null ? " (`" + Id + "`)" : "") +
            (Address != null ? " at `" + Address + "`" : "");
    }
}


public enum LatviaPostItemType
{
    PostBox,
    Office,
    ParcelLocker,
    CircleK,
    ServiceOnRequest
}