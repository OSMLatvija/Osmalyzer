using System.Linq;

namespace Osmalyzer
{
    public class OrMatch : OsmFilter
    {
        public override bool ForNodesOnly => _filters.All(f => f.ForNodesOnly);
        public override bool ForWaysOnly => _filters.All(f => f.ForWaysOnly);
        public override bool ForRelationsOnly => _filters.All(f => f.ForRelationsOnly);
        public override bool TaggedOnly => _filters.All(f => f.TaggedOnly);


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