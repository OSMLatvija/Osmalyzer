using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer
{
    public class HasAnyValue : OsmFilter
    {
        public override bool ForNodesOnly => false;
        public override bool ForWaysOnly => false;
        public override bool ForRelationsOnly => false;
        public override bool TaggedOnly => true;


        private readonly string _key;
        private readonly List<string> _values;


        public HasAnyValue(string key, List<string> values)
        {
            _key = key;
            _values = values;
        }


        internal override bool Matches(OsmElement element)
        {
            return 
                element.HasAnyTags &&
                _values.Any(v => element.HasValue(_key, v));
        }
    }
}