using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class LatvijasAutobussAnalyzer : PublicTransportAnalyzer<LatvijasAutobussAnalysisData>
    {
        public override string Name => "Latvijas Sabiedriskais Autobuss";

        public override string Description => "This checks the public transport route issues for " + Name;

        
        protected override string Label => "LSA";
    }
}