using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class RigasSatiksmeAnalyzer : PublicTransportAnalyzer<RigasSatiksmeAnalysisData>
{
    public override string Name => "Rigas Satiksme";

        
    protected override string Label => "RS";
}