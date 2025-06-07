namespace Osmalyzer;

/// <summary>
/// Represents a single "page" of analysed data in Osmalyzer.
/// </summary>
public abstract class Analyzer
{
    public abstract string Name { get; }

    public abstract string Description { get; }
    // todo: move this to report entry, but as overview or something?
    
    public abstract AnalyzerGroup Group { get; }


    /// <summary>
    /// Which data types are required for this analyzer to run?
    /// </summary>
    /// <returns></returns>
    public abstract List<Type> GetRequiredDataTypes();

    public abstract void Run(IReadOnlyList<AnalysisData> datas, Report report);
}