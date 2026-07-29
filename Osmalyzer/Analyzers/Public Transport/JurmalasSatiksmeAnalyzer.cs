namespace Osmalyzer;

[UsedImplicitly]
public class JurmalasSatiksmeAnalyzer : PublicTransportAnalyzer<JurmalasSatiksmeAnalysisData>
{
    public override string Name => "Jurmalas Autobusu Satiksme";

        
    protected override string Label => "JS";

    protected override bool CheckStopBoardingTypes => false;
    
    
    protected override void CleanUpGtfsData(GTFSNetwork gtfsNetwork)
    {
        // We don't have any known global issues
    }
}