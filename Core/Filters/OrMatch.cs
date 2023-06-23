using System.Linq;

namespace Osmalyzer
{
    public class OrMatch : OsmFilter
    {
        private readonly OsmFilter[] _filters;

        
        public OrMatch(params OsmFilter[] filters)
        {
            _filters = filters;
        }
        
        
        internal override bool Matches(OsmElement element)
        {
            return _filters.Any(f => f.Matches(element));
        }
    }
}