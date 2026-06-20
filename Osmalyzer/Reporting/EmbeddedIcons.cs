namespace Osmalyzer;

public static class EmbeddedIcons
{
    public static readonly List<EmbeddedIcon> Icons =
    [
        new LeafletIcon(
            "greenCheckmark.png",
            16,
            LeafletIcon.IconGroup.Main,
            ColorGroup.Green,
            MapPointStyle.Okay,
            MapPointStyle.CorrelatorPairMatched, MapPointStyle.CorrelatorLoneOsmMatched
        ),

        new LeafletIcon(
            "orangeCheckmark.png",
            16,
            LeafletIcon.IconGroup.Main,
            ColorGroup.Orange,
            MapPointStyle.Dubious,
            MapPointStyle.CorrelatorPairMatchedFar
        ),

        new LeafletIcon(
            "redCross.png",
            16,
            LeafletIcon.IconGroup.Main,
            ColorGroup.Red,
            MapPointStyle.Problem,
            MapPointStyle.CorrelatorItemUnmatched
        ),

        new LeafletIcon(
            "purpleQuestion.png",
            16,
            LeafletIcon.IconGroup.Main,
            ColorGroup.Purple,
            MapPointStyle.Okay,
            MapPointStyle.CorrelatorOsmUnmatched
        ),

        new LeafletIcon(
            "redExclamation.png",
            16,
            LeafletIcon.IconGroup.Main,
            ColorGroup.Red,
            MapPointStyle.Okay,
            MapPointStyle.CorrelatorLoneOsmUnmatched
        ),

        new LeafletIcon(
            "grayIgnored.png",
            16,
            LeafletIcon.IconGroup.Main,
            ColorGroup.Other,
            MapPointStyle.CorrelatorOsmIgnored
        ),

        new LeafletIcon(
            "blueQuestion.png",
            16,
            LeafletIcon.IconGroup.Main,
            ColorGroup.Other,
            MapPointStyle.Suggestion
        ),

        new LeafletIcon(
            "blueStar.png",
            12,
            LeafletIcon.IconGroup.Sub,
            ColorGroup.Other, // we don't expect it clustered in Sub group
            MapPointStyle.CorrelatorPairMatchedOffsetOrigin, MapPointStyle.CorrelatorPairMatchedFarOrigin
        ),

        new LeafletIcon(
            "greenBus.png",
            16,
            LeafletIcon.IconGroup.Main,
            ColorGroup.Other, // we don't expect it to be clustered
            MapPointStyle.BusStopMatchedWell
        ),

        new LeafletIcon(
            "orangeBus.png",
            16,
            LeafletIcon.IconGroup.Main,
            ColorGroup.Other, // we don't expect it to be clustered
            MapPointStyle.BusStopMatchedPoorly
        ),

        new LeafletIcon(
            "blueBus.png",
            16,
            LeafletIcon.IconGroup.Main,
            ColorGroup.Other, // we don't expect it to be clustered
            MapPointStyle.BusStopOriginalUnmatched
        ),

        new LeafletIcon(
            "purpleBus.png",
            16,
            LeafletIcon.IconGroup.Main,
            ColorGroup.Other, // we don't expect it to be clustered
            MapPointStyle.BusStopOsmUnmatched
        ),


        new LeafletClusterIcon("grayCircle.png", 20),
        new LeafletClusterIcon("redCircle.png", 20),
        new LeafletClusterIcon("orangeCircle.png", 20),
        new LeafletClusterIcon("greenCircle.png", 20),
        new LeafletClusterIcon("redGreenCircle.png", 20),
        new LeafletClusterIcon("redOrangeGreenCircle.png", 20),
        new LeafletClusterIcon("redOrangeCircle.png", 20),
        new LeafletClusterIcon("orangeGreenCircle.png", 20),
        new LeafletClusterIcon("redPurpleCircle.png", 20),
        new LeafletClusterIcon("greenPurpleCircle.png", 20),
        new LeafletClusterIcon("orangePurpleCircle.png", 20),
        new LeafletClusterIcon("purpleOrangeGreenCircle.png", 20),
        new LeafletClusterIcon("redPurpleGreenCircle.png", 20),
        new LeafletClusterIcon("redOrangePurpleCircle.png", 20),
        new LeafletClusterIcon("redOrangeGreenPurpleCircle.png", 20),

        new EmbeddedIcon(
            "editLinkPencil.svg",
            12
        )

    ];
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
    Orange,
    Red,
    Purple,
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