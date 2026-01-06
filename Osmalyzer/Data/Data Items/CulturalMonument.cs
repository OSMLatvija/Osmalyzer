using WikidataSharp;

namespace Osmalyzer;

public class CulturalMonument : IDataItem, IHasWikidataItem
{
    public OsmCoord Coord { get; }

    public string Name { get; }
    
    public int? ReferenceID { get; }

    public WikidataItem? WikidataItem { get; set; }

    public string? SourceFileVariant { get; }


    public CulturalMonument(OsmCoord coord, string name, int? referenceId, string? sourceFileVariant)
    {
        Coord = coord;
        Name = name;
        ReferenceID = referenceId;
        SourceFileVariant = sourceFileVariant;
    }

    
    public string ReportString()
    {
        return 
            "Cultural monument " +
            (ReferenceID != null ? "https://mantojums.lv/" + ReferenceID : "#???") + 
            // https://mantojums.lv/cultural-objects/### for system ID and https://mantojums.lv/### for reference ID
            " \"" + Name + "\" " +
            (WikidataItem != null ? " " + WikidataItem.WikidataUrl : "");
    }


    public override string ToString()
    {
        return "\"" + Name + "\" " +
               "#" + (ReferenceID?.ToString() ?? "?") + 
               " at " + Coord + 
               (SourceFileVariant != null ? " (var " + SourceFileVariant + ")" : "");
    }
}