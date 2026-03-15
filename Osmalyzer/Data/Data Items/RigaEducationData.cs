namespace Osmalyzer;

public class RigaEducationData : IDataItem
{
    public string Name { get; }

    public string Address { get; }

    public RigaEducationType Type { get; }

    public OsmCoord Coord { get; }


    public RigaEducationData(string name, string address, RigaEducationType type, OsmCoord coord)
    {
        Name = name;
        Address = address;
        Type = type;
        Coord = coord;
    }


    [Pure]
    public string ReportString()
    {
        return
            TypeLabel(Type) + " " +
            "`" + Name + "` " +
            "(" + Address + ")";
    }


    [Pure]
    public static string TypeLabel(RigaEducationType type)
    {
        return type switch
        {
            RigaEducationType.School    => "Riga school",
            RigaEducationType.Preschool => "Riga preschool",
            _                           => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }


    [Pure]
    public static string TypeLabelPlural(RigaEducationType type)
    {
        return type switch
        {
            RigaEducationType.School    => "Riga schools",
            RigaEducationType.Preschool => "Riga preschools",
            _                           => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}


public enum RigaEducationType
{
    School,
    Preschool
}

