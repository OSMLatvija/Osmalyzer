using System.Collections.Generic;

namespace Osmalyzer
{
    public class Report
    {
        public List<string> lines = new List<string>();
        
        
        public void WriteLine(string line)
        {
            lines.Add(line);
        }
    }
}