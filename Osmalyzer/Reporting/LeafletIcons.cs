using System.Collections.Generic;

namespace Osmalyzer;

public static class LeafletIcons
{
    public static readonly List<LeafletIcon> Icons = new List<LeafletIcon>()
    {
        new LeafletIcon("greenCheckmark", 16, LeafletIcon.IconGroup.Main, MapPointStyle.Okay, MapPointStyle.Info),
        new LeafletIcon("orangeCheckmark", 16, LeafletIcon.IconGroup.Main, MapPointStyle.Dubious),
        new LeafletIcon("redCross", 16, LeafletIcon.IconGroup.Main, MapPointStyle.Problem),
        new LeafletIcon("redQuestion", 16, LeafletIcon.IconGroup.Main, MapPointStyle.Unwanted),
        new LeafletIcon("blueStar", 12, LeafletIcon.IconGroup.Sub, MapPointStyle.Expected)
    };
}

public class LeafletIcon
{
    /// <summary>
    /// Matches the resource file name and will match Leaflet variable name
    /// </summary>
    public string Name { get; }
    
    public int Size { get; }

    public IconGroup Group { get; }
    
    public MapPointStyle[] Styles { get; }


    public LeafletIcon(string name, int size, IconGroup group, params MapPointStyle[] styles)
    {
        Name = name;
        Size = size;
        Group = group;
        Styles = styles;
    }

    
    public enum IconGroup
    {
        Main,
        Sub
    }
}