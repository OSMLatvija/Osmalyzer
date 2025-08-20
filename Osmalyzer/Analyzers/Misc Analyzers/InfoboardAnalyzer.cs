namespace Osmalyzer;

[UsedImplicitly]
public class InfoboardAnalyzer : Analyzer
{
    public override string Name => "Infoboards";

    public override string Description => "This report checks information boards.";

    public override AnalyzerGroup Group => AnalyzerGroup.POIs;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData) ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;
        
        OsmDataExtract osmElements = osmMasterData.Filter(
            new HasValue("tourism", "information"),
            new HasAnyValue("information", "board", "map"),
            new DoesntHaveValue("board_type", "welcome_sign"), // not an actual board (likely)
            new DoesntHaveValue("board_type", "public_transport"), // basically, departures/routes board/timetable
            new DoesntHaveValue("board_type", "notice"), // notice board, although should be `advertising=board`
            new DoesntHaveValue("indoor", "yes"), // ignoring indoor maps and such
            new DoesntHaveKey("level"), // implies indoor
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.Fuzzy)
        );

        // Prepare groups

        report.AddGroup(ReportGroup.Photos, "Infoboards with photos");

        // report.AddEntry(
        //     ReportGroup.NonDefining,
        //     new DescriptionReportEntry(
        //         ""
        //     )
        // );
        
        report.AddGroup(ReportGroup.UnknownTag, "Infoboards with unknown tags");

        // Parse
        
        foreach (OsmElement element in osmElements.Elements)
        {
            bool common = element.HasKey("wikimedia_commons");
            bool mapillary = element.HasKey("mapillary");
            bool panoramax = element.HasKey("panoramax");
            bool image = element.HasKey("image");
            
            bool any = common || mapillary || panoramax || image;
            
            if (any)
            {
                report.AddEntry(
                    ReportGroup.Photos,
                    new MapPointReportEntry(
                        element.AverageCoord,
                        "Infoboard with photo",
                        element,
                        MapPointStyle.Okay
                    )
                );
            }
            else
            {
                report.AddEntry(
                    ReportGroup.Photos,
                    new MapPointReportEntry(
                        element.AverageCoord,
                        "Infoboard with no photo",
                        element,
                        MapPointStyle.Problem
                    )
                );
            }
        }
    }

    
    private enum ReportGroup
    {
        Photos,
        UnknownTag
    }
}