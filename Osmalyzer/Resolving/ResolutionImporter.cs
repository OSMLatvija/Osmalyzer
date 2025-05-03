namespace Osmalyzer;

public abstract class ResolutionImporter
{
    public List<ImportedResolution> Import(IList<IAnalyzerWithResolutions> analyzers)
    {
        List<ImportedResolution> resolutions = new List<ImportedResolution>();
        
        foreach (List<string?> data in ChildImportData())
        {
            ImportedResolution? resolution = ImportedResolution.Import(data, analyzers);

            if (resolution != null) // incompatible (invalid would exception)
                resolutions.Add(resolution);
        }

        return resolutions;
    }

    
    protected abstract IEnumerable<List<string?>> ChildImportData();
}