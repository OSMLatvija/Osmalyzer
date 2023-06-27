using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class JurmalasSatiksmeAnalyzer : PublicTransportAnalyzer<JurmalasSatiksmeAnalysisData>
    {
        public override string Name => "Jurmalas Autobusu Satiksme";

        public override string? Description => null;

        
        protected override string Label => "JS";
    }
}