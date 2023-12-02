using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class JurmalasSatiksmeAnalyzer : PublicTransportAnalyzer<JurmalasSatiksmeAnalysisData>
{
    public override string Name => "Jurmalas Autobusu Satiksme";

        
    protected override string Label => "JS";
}