namespace Osmalyzer;

public abstract class Analyzer
{
    public abstract string Name { get; }

    public abstract string Description { get; }
    // todo: move this to report entry, but as overview or something?
    
    public abstract AnalyzerGroup Group { get; }


    public abstract List<Type> GetRequiredDataTypes();

    public abstract void Run(IReadOnlyList<AnalysisData> datas, Report report);
}