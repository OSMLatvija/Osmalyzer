using System;
using System.IO;
using JetBrains.Annotations;

namespace Osmalyzer
{
    public abstract class AnalysisData
    {
        public const string cacheBasePath = "cache/";
        
        public const string cacheRevisionFilePath = cacheBasePath + "cache-v2.txt"; // just has to be unique to previous one(s)
        

        public abstract string Name { get; }
        // todo: page name not from this - some sort of internal id

        public DateTime? DataDate => _dataDate;

        
        /// <summary>
        /// Unique short ID used for file names
        /// </summary>
        protected abstract string DataFileIdentifier { get; }


        private DateTime? _dataDate;


        public void Retrieve()
        {
            if (this is ICachableAnalysisData cachableAnalysisData)
            {
                _dataDate = GetDataDateFromMetadataFile();

                if (_dataDate != null)
                {
                    Console.WriteLine("Getting cache date...");
                    DateTime newDataDate = cachableAnalysisData.RetrieveDataDate();

                    if (DataDate < newDataDate)
                    {
                        Console.WriteLine("Downloading (cache out of date)...");
                        Download();
                    }
                    else
                    {
                        Console.WriteLine("Using cached files.");
                    }

                    StoreDataDate(newDataDate);
                }
                else
                {
                    Console.WriteLine("Getting cache date...");
                    DateTime newDataDate = cachableAnalysisData.RetrieveDataDate();

                    Console.WriteLine("Downloading (not yet cached)...");
                    Download();
                    
                    StoreDataDate(newDataDate);
                }
            }
            else
            {
                Console.WriteLine("Downloading (no cache)...");
                Download();
            }
        }

        
        protected abstract void Download();


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
}