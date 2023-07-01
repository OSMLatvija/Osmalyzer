using System;
using System.IO;

namespace Osmalyzer
{
    public abstract class AnalysisData
    {
        public abstract string Name { get; }
        // todo: page name not from this - some sort of internal id

        public abstract string? DataDateFileName { get; }

        public DateTime? DataDate => DataDateFileName != null ? _dataDate ??= GetDataDateFromMetadataFile() : null;

        public abstract bool? DataDateHasDayGranularity { get; }


        private DateTime? _dataDate;


        public abstract void Retrieve();

        
        protected void StoreDataDate(DateTime newDate)
        {
            _dataDate = newDate;
            
            File.WriteAllText(DataDateFileName!, _dataDate.Value.Ticks.ToString());
        }

        protected void ClearDataDate()
        {
            if (File.Exists(DataDateFileName!))
                File.Delete(DataDateFileName!);
        }


        private DateTime? GetDataDateFromMetadataFile()
        {
            if (!File.Exists(DataDateFileName!))
                return null;
            
            string dataDateString = File.ReadAllText(DataDateFileName!);
            
            return new DateTime(long.Parse(dataDateString));
        }
    }
}