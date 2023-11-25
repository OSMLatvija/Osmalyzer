using System.Collections.Generic;

namespace Osmalyzer;

public static class LeafletIcons
{
    public static readonly List<LeafletIcon> Icons = new List<LeafletIcon>()
    {
        new LeafletIcon("greenCheckmark", 16, MapPointStyle.Okay, MapPointStyle.Info),
        new LeafletIcon("orangeCheckmark", 16, MapPointStyle.Dubious),
        new LeafletIcon("redCross", 16, MapPointStyle.Problem),
        new LeafletIcon("blueStar", 12, MapPointStyle.Expected)
    };
}

public class LeafletIcon
{
    /// <summary>
    /// Matches the resource file name and will match Leaflet variable name
    /// </summary>
    public string Name { get; }
    
    public int Size { get; }
    
    public MapPointStyle[] Styles { get; }


    public LeafletIcon(string name, int size, params MapPointStyle[] styles)
    {
        Name = name;
        Size = size;
        Styles = styles;
    }
}