namespace Osmalyzer;

public interface IAnalyzerWithResolutions
{
    /// <summary> Unique stable id for the analyzer to specify its <see cref="Resolvable"/>s. </summary>
    string ResolutionAnalyzerID { get; }
}