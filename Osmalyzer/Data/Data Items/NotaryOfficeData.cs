namespace Osmalyzer;

/// <summary>
/// Represents an active notary office (place of practice)
/// </summary>
public class NotaryOfficeData : IDataItem
{
    public OsmCoord Coord { get; }

    public string Name { get; }

    public string Address { get; }

    public string FullAddress { get; }

    public string City { get; }

    public string Court { get; }

    public string? Phone { get; }

    public string? Email { get; }

    public string? OpeningHours { get; }

    public List<string> Languages { get; }

    public string Website { get; }


    public NotaryOfficeData(
        OsmCoord coord,
        string name,
        string address,
        string fullAddress,
        string city,
        string court,
        string? phone,
        string? email,
        string? openingHours,
        List<string> languages,
        string website)
    {
        Coord = coord;
        Name = name;
        Address = address;
        FullAddress = fullAddress;
        City = city;
        Court = court;
        Phone = phone;
        Email = email;
        OpeningHours = openingHours;
        Languages = languages;
        Website = website;
    }

    
    [Pure]
    public string ReportString() => ReportString(false);

    [Pure]
    public string ReportString(bool full)
    {
        return
            "Notary `" + Name + "` " +
            "at `" + FullAddress + "` " +
            "for `" + Court + "` " +
            (full && Phone != null ? "phone `" + Phone + "` " : "") +
            (full && Email != null ? "email `" + Email + "` " : "") +
            (full && OpeningHours != null ? "hours `" + OpeningHours + "` " : "") +
            (full ? "langs " + string.Join(", ", Languages.Select(l => "`" + l + "`")) : "");
    }

    public override string ToString() => ReportString(true);
}

