using System;
using System.IO;
using JetBrains.Annotations;

namespace Osmalyzer;

public abstract class AnalysisData
{
    public const string cacheBasePath = "cache/";
        
    public const string cacheRevisionFilePath = cacheBasePath + "cache-v2.txt"; // just has to be unique to previous one(s)

    private const int undatedDataCachingGracePeriod = 2 * 60 * 60; // sec


    public abstract string Name { get; }
    // todo: page name not from this - some sort of internal id

    /// <summary>
    /// A link for the human reading the report, so it takes them approximately to where the data lives.
    /// In many cases, this won't be actual data like .zip files or .xml sources, because that's only parsable.
    /// </summary>
    public abstract string? ReportWebLink { get; }

    
    public DateTime? DataDate => _dataDate;
        
    public DataRetrievalStatus RetrievalStatus { get; private set; }

        
    /// <summary>
    /// Unique short ID used for file names
    /// </summary>
    protected abstract string DataFileIdentifier { get; }


    private DateTime? _dataDate;


    public void Retrieve()
    {
#if REMOTE_EXECUTION
        try
        {
#endif
        
            DoRetrieve();
        
#if REMOTE_EXECUTION
        }
        catch (Exception)
        {
            // On remote, we continue gracefully

            Console.WriteLine("Failed with exception!");

            RetrievalStatus = DataRetrievalStatus.Fail;
            return;
        }
#endif        

        RetrievalStatus = DataRetrievalStatus.Ok;
    }


    protected abstract void Download();


    private void DoRetrieve()
    {
        switch (this)
        {
            case IDatedAnalysisData cachableAnalysisData:
            {
                _dataDate = GetDataDateFromMetadataFile();

                if (_dataDate != null)
                {
                    Console.WriteLine("Getting dated cache date...");
                    DateTime newDataDate = cachableAnalysisData.RetrieveDataDate();

                    if (_dataDate < newDataDate)
                    {
                        Console.WriteLine("Downloading (dated cache out of date)...");
                        Download();
                    }
                    else
                    {
                        Console.WriteLine("Using dated cached files.");
                    }

                    StoreDataDate(newDataDate);
                }
                else
                {
                    Console.WriteLine("Getting dated cache date...");
                    DateTime newDataDate = cachableAnalysisData.RetrieveDataDate();

                    Console.WriteLine("Downloading (not yet cached with date)...");
                    Download();

                    StoreDataDate(newDataDate);
                }

                break;
            }

            case IUndatedAnalysisData:
            {
                _dataDate = GetDataDateFromMetadataFile();

                if (_dataDate != null)
                {
                    if (_dataDate.Value.AddSeconds(undatedDataCachingGracePeriod) < DateTime.UtcNow)
                    {
                        Console.WriteLine("Downloading (undated cache out of grace period)...");
                        Download();
                        
                        StoreDataDate(DateTime.UtcNow);
                    }
                    else
                    {
                        Console.WriteLine("Using undated cached files.");
                    }
                }
                else
                {
                    Console.WriteLine("Downloading (not yet cached without date)...");
                    Download();

                    StoreDataDate(DateTime.UtcNow);
                }
                
                break;
            }

            default:
            {
                Console.WriteLine("Downloading (non-cachable)...");
                Download();
                break;
            }
        }
    }
    
    private DateTime? GetDataDateFromMetadataFile()
    {
        string cachedDateFileName = CachedDateFileName();
            
        if (!File.Exists(cachedDateFileName))
            return null;
            
        string dataDateString = File.ReadAllText(cachedDateFileName);
            
        return new DateTime(long.Parse(dataDateString));
    }


    private void StoreDataDate(DateTime newDate)
    {
        _dataDate = newDate;
            
        File.WriteAllText(CachedDateFileName(), _dataDate.Value.Ticks.ToString());
    }

    [Pure]
    private string CachedDateFileName()
    {
        return cacheBasePath + DataFileIdentifier + "-cache-date.txt";
    }
}
    
    
public enum DataRetrievalStatus
{
    Ok,
    Fail
}