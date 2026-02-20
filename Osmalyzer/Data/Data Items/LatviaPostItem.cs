namespace Osmalyzer;

public class LatviaPostItem : IDataItem
{
    public LatviaPostItemType ItemType { get; }

    public string? Code { get; }

    public string? Name { get; }

    public string? Address { get; }
    
    public OsmCoord Coord { get; }

    public bool Unisend { get; } // todo: make a parcel locked class so this isn't shared

    /// <summary>
    /// As opposed to full post office.
    /// </summary>
    public bool ClientCenter { get; } // todo: make a post office class so this isn't shared


    public LatviaPostItem(LatviaPostItemType itemType, string? name, string? address, string? code, OsmCoord coord, bool unisend, bool clientCenter)
    {
        ItemType = itemType;
        Name = name;
        Address = address;
        Code = code;
        Coord = coord;
        Unisend = unisend;
        ClientCenter = clientCenter;
    }


    [Pure]
    public ParcelLocker AsParcelLocker()
    {
        if (ItemType != LatviaPostItemType.ParcelLocker) throw new Exception("This item is not a parcel locker.");
        
        return new ParcelLocker(
            "Latvijas Pasts", // todo: shouldn't this be Unisend for Unisend lockers?
            Code,
            Name,
            Address,
            Coord
        );
    }

    public string ReportString()
    {
        return
            TypeToLabel(ItemType, Unisend, ClientCenter) +
            (Name != null ? " `" + Name + "`" : "") +
            (Code != null ? " (`" + Code + "`)" : "") +
            (Address != null ? " at `" + Address + "`" : "");

        
        [Pure]
        static string TypeToLabel(LatviaPostItemType itemType, bool unisend, bool clientCenter)
        {
            return itemType switch
            {
                LatviaPostItemType.PostBox          => "Post box",
                LatviaPostItemType.Office           => clientCenter ? "Client center" : "Post office",
                LatviaPostItemType.ParcelLocker     => unisend ? "Unisend locker" : "Parcel locker",

                _ => throw new NotImplementedException()
            };
        }
    }
}


public enum LatviaPostItemType
{
    Office,
    PostBox,
    ParcelLocker
}