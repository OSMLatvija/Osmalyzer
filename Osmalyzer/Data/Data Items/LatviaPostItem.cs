namespace Osmalyzer;

public class LatviaPostItem : IDataItem
{
    public string? Name { get; }

    public string? Code { get; }

    public string? Address { get; }

    public LatviaPostItemType? ItemType { get; }
    
    public OsmCoord Coord { get; }


    public LatviaPostItem(string? name, string? address, string? code, LatviaPostItemType itemType, OsmCoord coord)
    {
        Name = name;
        Address = address;
        Code = code;
        ItemType = itemType;
        Coord = coord;
    }
    
    
    public string ReportString()
    {
        return
            "Latvia Post " + ItemType.ToString() +
            (Name != null ? " `" + Name + "`" : "") +
            (Code != null ? " (`" + Code + "`)" : "") +
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