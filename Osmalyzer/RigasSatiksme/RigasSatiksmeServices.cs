using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace Osmalyzer
{
    public class RigasSatiksmeServices
    {
        public IEnumerable<RigasSatiksmeService> Services => _services.AsReadOnly();

        
        private readonly List<RigasSatiksmeService> _services;

        
        public RigasSatiksmeServices(string dataFileName)
        {
            _services = new List<RigasSatiksmeService>();

            string[] lines = File.ReadAllLines(dataFileName);

            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0) // header row
                    continue;
            
                string line = lines[i];
                // service_id,monday,tuesday,wednesday,thursday,friday,saturday,sunday,start_date,end_date
                // 24837,0,0,0,0,0,1,1,20230415,20240401

                string[] segments = line.Split(',');

                // service_id - 24837
                // monday - 0
                // tuesday - 0
                // wednesday - 0
                // thursday - 0
                // friday - 0
                // saturday - 1
                // sunday - 1
                // start_date - 20230415
                // end_date - 20240401

                string serviceId = segments[0];

                RigasSatiksmeService service = new RigasSatiksmeService(serviceId);
                _services.Add(service);
            }
        }

        
        [Pure]
        public RigasSatiksmeService GetService(string id)
        {
            return _services.First(t => t.Id == id);
        }
    }
}