namespace Osmalyzer;

[UsedImplicitly]
public class RigasSatiksmeStopAnalyzer : PublicTransportStopAnalyzer<RigasSatiksmeAnalysisData>
{
    protected override string Label => "Rigas Satiksme";
    
    protected override bool CheckStopBoardingTypes => true;

    protected override string OsmTaggingValueForOperator => "Rīgas satiksme";
    protected override string OsmTaggingValueForOperatorWikidata => "Q2280274";
    protected override OsmCoord? StopMatchingCenter => new OsmCoord(56.9496, 24.1052); // Riga center
    protected override double? StopMatchingDistance => 15500; // some stops are ~15km away like Jaunmārupe, Vecāķi, Dārziņi
    protected override string GtfsDisambiguatorId => "LV-RS";
    protected override bool RegionalService => false;
}