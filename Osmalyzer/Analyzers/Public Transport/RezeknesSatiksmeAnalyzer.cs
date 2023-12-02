using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class RezeknesSatiksmeAnalyzer : PublicTransportAnalyzer<RezeknesSatiksmeAnalysisData>
{
    public override string Name => "Rezeknes Satiksme";

        
    protected override string Label => "RS";
}