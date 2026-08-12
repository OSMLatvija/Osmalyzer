namespace Osmalyzer;

[UsedImplicitly]
public class RigasSatiksmeStopAnalyzer : PublicTransportStopAnalyzer<RigasSatiksmeAnalysisData>
{
    public override string Name => "Rigas Satiksme";

        
    protected override string Label => "RS";
    
    protected override bool CheckStopBoardingTypes => true;

    protected override string OsmTaggingValueForOperator => "Rīgas satiksme";
    protected override string OsmTaggingValueForOperatorWikidata => "Q2280274";
    protected override OsmCoord? StopMatchingCenter => new OsmCoord(56.9496, 24.1052); // Riga center
    protected override double? StopMatchingDistance => 15500; // some stops are ~15km away like Jaunmārupe, Vecāķi, Dārziņi
    protected override string GtfsDisambiguatorId => "LV-RS";
    protected override bool RegionalService => false;
}