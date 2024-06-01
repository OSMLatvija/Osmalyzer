using System;

namespace Osmalyzer;

public class LatviaPostItem : IDataItem
{
    public LatviaPostItemType ItemType { get; }

    public string? Code { get; }

    public string? Name { get; }

    public string? Address { get; }
    
    public OsmCoord Coord { get; }

    
    public LatviaPostItem(LatviaPostItemType itemType, string? name, string? address, string? code, OsmCoord coord)
    {
        Code = code;
        Name = name;
        Address = address;
        Coord = coord;
        ItemType = itemType;
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
    

    public string ReportString()
    {
        return
            TypeToLabel(ItemType) +
            (Name != null ? " `" + Name + "`" : "") +
            (Code != null ? " (`" + Code + "`)" : "") +
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