using System.Linq;

namespace Osmalyzer
{
    public class OrMatch : OsmFilter
    {
        public override bool ForNodesOnly => false;
        public override bool ForWaysOnly => false;
        public override bool ForRelationsOnly => false;


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