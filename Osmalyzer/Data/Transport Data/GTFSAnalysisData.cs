namespace Osmalyzer;

public abstract class GTFSAnalysisData : AnalysisData, IDatedAnalysisData
{
    public abstract string ExtractionFolder { get; }

    public abstract bool DataDateHasDayGranularity { get; }

    public override bool NeedsPreparation => true;

    public GTFSNetwork Network { get; private set; } = null!; // only null until prepared

        
    protected abstract string DataURL { get; }

    protected abstract string DataFileName { get; }

    protected virtual bool NeedsBrowserDownload => false;


    public DateTime? RetrieveDataDate()
    {
        if (NeedsBrowserDownload)
            return null; // TODO: implement this? I am not sure browser header reading is a "thing"
            
        return WebsiteDownloadHelper.ReadHeaderDate(DataURL);
    }

    protected override void Download()
    {
        if (NeedsBrowserDownload)
        {
            WebsiteBrowsingHelper.DownloadPage(
                DataURL,
                DataFileName
            );
        }
        else
        {
            WebsiteDownloadHelper.Download(
                DataURL,
                DataFileName
            );
        }
    }

    protected override void DoPrepare()
    {
        // GTFS data comes in a zip file, so unzip
            
        ZipHelper.ExtractZipFile(
            DataFileName,
            ExtractionFolder
        );
        
        Network = new GTFSNetwork(Path.GetFullPath(ExtractionFolder));
        
        // Clean up GTFS data issues specific to this operator
        CleanUpGtfsData();
    }

    
    /// <summary>
    /// Apply operator-specific cleanup to the GTFS data.
    /// </summary>
    protected abstract void CleanUpGtfsData();
}