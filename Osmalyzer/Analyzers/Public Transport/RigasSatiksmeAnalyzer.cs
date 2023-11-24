using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class RigasSatiksmeAnalyzer : PublicTransportAnalyzer<RigasSatiksmeAnalysisData>
{
    public override string Name => "Rigas Satiksme";

    public override string Description => "This checks the public transport route issues for " + Name;

        
    protected override string Label => "RS";
}