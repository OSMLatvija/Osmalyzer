namespace Osmalyzer;

public class LatviaPostItem : IDataItem
{
    public LatviaPostItemType ItemType { get; }

    public string? Code { get; }

    public int? CodeValue { get; }

    public string? Name { get; }

    public string? Address { get; }
    
    public OsmCoord Coord { get; }

    
    public LatviaPostItem(LatviaPostItemType itemType, string? name, string? address, string? code, int? codeValue, OsmCoord coord)
    {
        ItemType = itemType;
        Name = name;
        Address = address;
        Code = code;
        CodeValue = codeValue;
        Coord = coord;
    }


    [Pure]
    public ParcelLocker AsParcelLocker()
    {
        if (ItemType != LatviaPostItemType.ParcelLocker) throw new Exception("This item is not a parcel locker.");
        
        return new ParcelLocker(
            "Latvijas Pasts",
            Code,
            Name,
            Address,
            Coord
        );
    }

    [Pure]
    public ParcelPickupPoint AsPickupPointLocker()
    {
        if (ItemType != LatviaPostItemType.CircleK) throw new Exception("This item is not a pickup point.");

        return new ParcelPickupPoint(
            "Latvijas Pasts",
            Code,
            Name,
            Address,
            Coord,
            "Circle K" // the only place in Latvia (at the moment) 
        );
    }


    public string ReportString()
    {
        return
            TypeToLabel(ItemType) +
            (Name != null ? " `" + Name + "`" : "") +
            (CodeValue != null ? " (#`" + CodeValue + "`)" : Code != null ? " (`" + Code + "`)" : "") +
            (Address != null ? " at `" + Address + "`" : "");

        
        [Pure]
        static string TypeToLabel(LatviaPostItemType itemType)
        {
            return itemType switch
            {
                LatviaPostItemType.PostBox          => "Post box",
                LatviaPostItemType.Office           => "Post office",
                LatviaPostItemType.ParcelLocker     => "Parcel locker",
                LatviaPostItemType.CircleK          => "Circle K service location",
                LatviaPostItemType.ServiceOnRequest => "Service-on-request location",

                _ => throw new NotImplementedException()
            };
        }
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