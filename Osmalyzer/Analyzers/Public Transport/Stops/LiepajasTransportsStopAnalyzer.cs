namespace Osmalyzer;

[UsedImplicitly]
public class LiepajasTransportsStopAnalyzer : PublicTransportStopAnalyzer<LiepajasTransportsAnalysisData>
{
    public override string Name => "Liepajas Sabiedriskais Transports";


    protected override string Label => "LST";

    protected override bool CheckStopBoardingTypes => false;

    protected override string OsmTaggingValueForOperator => "Liepājas sabiedriskais transports";
    protected override string? OsmTaggingValueForOperatorWikidata => null;
    protected override OsmCoord? StopMatchingCenter => null;
    protected override double? StopMatchingDistance => null;
    protected override string? GtfsDisambiguatorId => null;
    protected override bool RegionalService => false;
}