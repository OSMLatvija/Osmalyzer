using System;
using System.IO;

namespace Osmalyzer
{
    public abstract class AnalysisData
    {
        public abstract string Name { get; }
        // todo: page name not from this - some sort of internal id

        public abstract string? DataDateFileName { get; }

        public DateTime? DataDate => _dataDate;

        public abstract bool? DataDateHasDayGranularity { get; }


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
            if (!File.Exists(DataDateFileName!))
                return null;
            
            string dataDateString = File.ReadAllText(DataDateFileName!);
            
            return new DateTime(long.Parse(dataDateString));
        }


        private void StoreDataDate(DateTime newDate)
        {
            _dataDate = newDate;
            
            File.WriteAllText(DataDateFileName!, _dataDate.Value.Ticks.ToString());
        }
    }
}