using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class LiepajasTransportsAnalyzer : PublicTransportAnalyzer<LiepajasTransportsAnalysisData>
{
    public override string Name => "Liepajas Sabiedriskais Transports";

    public override string Description => "This checks the public transport route issues for " + Name;

        
    protected override string Label => "LST";
}