using System.Linq;

namespace Osmalyzer
{
    public class AndMatch : OsmFilter
    {
        private readonly OsmFilter[] _filters;

        
        public AndMatch(params OsmFilter[] filters)
        {
            _filters = filters;
        }
        
        
        internal override bool Matches(OsmElement element)
        {
            return _filters.All(f => f.Matches(element));
        }
    }
}