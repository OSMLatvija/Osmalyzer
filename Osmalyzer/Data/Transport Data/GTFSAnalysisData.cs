using System;

namespace Osmalyzer;

public abstract class GTFSAnalysisData : AnalysisData, IPreparableAnalysisData, IDatedAnalysisData
{
    public abstract string ExtractionFolder { get; }

    public abstract bool DataDateHasDayGranularity { get; }

        
    protected abstract string DataURL { get; }

    protected abstract string DataFileName { get; }

        
    public DateTime RetrieveDataDate()
    {
        return WebsiteDownloadHelper.ReadHeaderDate(DataURL)!.Value;
    }

    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            DataURL, 
            DataFileName
        );
    }

    public void Prepare()
    {
        // GTFS data comes in a zip file, so unzip
            
        ZipHelper.ExtractZipFile(
            DataFileName,
            ExtractionFolder
        );
    }
}