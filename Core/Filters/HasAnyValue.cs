using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer
{
    public class HasAnyValue : OsmFilter
    {
        public override bool ForNodesOnly => false;
        public override bool ForWaysOnly => false;
        public override bool ForRelationsOnly => false;


        private readonly string _tag;
        private readonly List<string> _values;


        public HasAnyValue(string tag, List<string> values)
        {
            _tag = tag;
            _values = values;
        }


        internal override bool Matches(OsmElement element)
        {
            return
                element.RawElement.Tags != null &&
                element.RawElement.Tags.Any(t => t.Key == _tag && _values.Contains(t.Value));
        }
    }
}