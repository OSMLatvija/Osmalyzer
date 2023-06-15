using System;
using System.IO;

namespace Osmalyzer
{
    public abstract class AnalysisData
    {
        public abstract string Name { get; }

        public abstract string DataFileName { get; }
        
        public abstract string? DataDateFileName { get; }

        public DateTime? DataDate => DataDateFileName != null ? _dataDate ??= GetDataDateFromMetadataFile() : null;

        public abstract bool? DataDateHasDayGranularity { get; }


        private DateTime? _dataDate;


        public abstract void Retrieve();

        public abstract void Prepare();

        
        protected void StoreDataDate(DateTime newDate)
        {
            _dataDate = newDate;
            
            File.WriteAllText(DataDateFileName!, _dataDate.Value.Ticks.ToString());
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