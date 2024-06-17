using System;
using System.Collections.ObjectModel;
using System.IO;

namespace Osmalyzer;

public abstract class AnalysisData
{
    private const string cacheRevisionFileName = "cache-v4.txt"; // just has to be unique to previous one(s), but I'm "counting" up for consistency

    private const string cacheBaseFolder = "cache";

    private const int undatedDataCachingGracePeriod = 2 * 60 * 60; // sec


    public static string CacheBasePath => Path.GetFullPath(cacheBaseFolder);
    
    public static string CacheRevisionFilePath => Path.Combine(CacheBasePath, cacheRevisionFileName);


    public abstract string Name { get; }
    // todo: page name not from this - some sort of internal id

    /// <summary>
    /// A link for the human reading the report, so it takes them approximately to where the data lives.
    /// In many cases, this won't be actual data like .zip files or .xml sources, because that's only parsable.
    /// </summary>
    public abstract string? ReportWebLink { get; }

    public abstract bool NeedsPreparation { get; }

    
    public DateTime? DataDate => _dataDate;
        
    public DataStatus Status { get; private set; }

        
    /// <summary>
    /// Unique short ID used for file names
    /// </summary>
    protected abstract string DataFileIdentifier { get; }

    
    private string CachedDateFilePath => Path.Combine(CacheBasePath, DataFileIdentifier + "-cache-date.txt");


    private DateTime? _dataDate;


    public void Retrieve()
    {
        try
        {
            DoRetrieve();
        }
        catch (Exception e)
        {
            PrintExceptionDetails(e, "retrieve");

            Status = DataStatus.FailedToRetrieve;

#if !REMOTE_EXECUTION
            throw; // on local, allow debugging the problem
#else
            return; // on remote, continue gracefully
#endif
        }

        Status = DataStatus.Ok;
    }

    public void Prepare()
    {
        try
        {
            DoPrepare();
        }
        catch (Exception e)
        {
            PrintExceptionDetails(e, "prepare");

            Status = DataStatus.FailedToPrepare;
            
#if !REMOTE_EXECUTION
            throw; // on local, allow debugging the problem
#else
            return; // on remote, continue gracefully
#endif
        }

        Status = DataStatus.Ok;
    }


    protected abstract void Download();
    
    protected abstract void DoPrepare();


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

                    StoreDataDate(newDataDate); // after download in case it fails
                }
                else
                {
                    Console.WriteLine("Getting dated cache date...");
                    DateTime newDataDate = cachableAnalysisData.RetrieveDataDate();

                    Console.WriteLine("Downloading (not yet cached with date)...");
                    Download();

                    StoreDataDate(newDataDate); // after download in case it fails
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
                        
                        StoreDataDate(DateTime.UtcNow); // after download in case it fails
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

                    StoreDataDate(DateTime.UtcNow); // after download in case it fails
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
        if (!File.Exists(CachedDateFilePath))
            return null;
            
        string dataDateString = File.ReadAllText(CachedDateFilePath);
            
        return new DateTime(long.Parse(dataDateString));
    }


    private void StoreDataDate(DateTime newDate)
    {
        _dataDate = newDate;
            
        File.WriteAllText(CachedDateFilePath, _dataDate.Value.Ticks.ToString());
    }

    private static void PrintExceptionDetails(Exception e, string operationLabel)
    {
        Console.WriteLine("Failed to " + operationLabel + " with exception!");

        if (e is AggregateException aggregateException)
        {
            ReadOnlyCollection<Exception> subs = aggregateException.Flatten().InnerExceptions;
            
            for (int i = 0; i < subs.Count; i++)
            {
                Exception sub = subs[i];
                PrintInnerExceptions(sub, "Aggregate #" + (i + 1));
            }
        }
        else
        {
            PrintInnerExceptions(e, null);
        }

        return;


        static void PrintInnerExceptions(Exception e, string? label)
        {
            Console.WriteLine((label != null ? label + " exception" : "Exception") + ": " + e.Message);

            while (e.InnerException != null)
            {
                e = e.InnerException;
                Console.WriteLine("Inner exception: " + e.Message);
            }
        }
    }
}
    
    
public enum DataStatus
{
    Ok,
    FailedToRetrieve,
    FailedToPrepare
}