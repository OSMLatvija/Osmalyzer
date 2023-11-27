using System.Collections.Generic;

namespace Osmalyzer;

public static class EmbeddedIcons
{
    public static readonly List<EmbeddedIcon> Icons = new List<EmbeddedIcon>()
    {
        new LeafletIcon(
            "greenCheckmark",
            16,
            LeafletIcon.IconGroup.Main,
            ColorGroup.Green,
            MapPointStyle.Okay,
            MapPointStyle.CorrelatorPairMatched, MapPointStyle.CorrelatorLoneOsmMatched
        ),
        
        new LeafletIcon(
            "orangeCheckmark", 
            16, 
            LeafletIcon.IconGroup.Main, 
            ColorGroup.Green,
            MapPointStyle.Dubious,
            MapPointStyle.CorrelatorPairMatchedFar
        ),
        
        new LeafletIcon(
            "redCross", 
            16, 
            LeafletIcon.IconGroup.Main, 
            ColorGroup.Red,
            MapPointStyle.Problem,
            MapPointStyle.CorrelatorItemUnmatched, MapPointStyle.CorrelatorOsmUnmatched
        ),
        
        new LeafletIcon(
            "redQuestion", 
            16, 
            LeafletIcon.IconGroup.Main, 
            ColorGroup.Red,
            MapPointStyle.Okay,
            MapPointStyle.CorrelatorLoneOsmUnmatched
        ),
        
        new LeafletIcon(
            "blueStar", 
            12, 
            LeafletIcon.IconGroup.Sub, 
            ColorGroup.Other, // we don't expect it clustered in Sub group
            MapPointStyle.CorrelatorPairMatchedOffsetOrigin, MapPointStyle.CorrelatorPairMatchedFarOrigin
        ),
        
        new LeafletClusterIcon(
            "grayCircle",
            20
        ),
        
        new LeafletClusterIcon(
            "redCircle",
            20
        ),
        
        new LeafletClusterIcon(
            "greenCircle",
            20
        ),
        
        new LeafletClusterIcon(
            "redGreenCircle",
            20
        )
    };
}

public class LeafletClusterIcon : EmbeddedIcon
{
    public LeafletClusterIcon(string name, int size)
        : base(name, size)
    {
    }
}
    
public class LeafletIcon : EmbeddedIcon
{
    public IconGroup Group { get; }
    
    public MapPointStyle[] Styles { get; }
    
    public ColorGroup ColorGroup { get; }


    public LeafletIcon(string name, int size, IconGroup group, ColorGroup colorGroup, params MapPointStyle[] styles)
        : base(name, size)
    {
        Group = group;
        ColorGroup = colorGroup;
        Styles = styles;
    }

    
    public enum IconGroup
    {
        Main,
        Sub
    }
}

public enum ColorGroup
{
    Green,
    Red,
    Other
}

public class EmbeddedIcon
{
    /// <summary>
    /// Matches the resource file name and will match Leaflet variable name
    /// </summary>
    public string Name { get; }

    public int Size { get; }

    
    public EmbeddedIcon(string name, int size)
    {
        Name = name;
        Size = size;
    }
}