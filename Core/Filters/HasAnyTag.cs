using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer
{
    public class HasAnyTag : OsmFilter
    {
        private readonly List<string> _tags;


        public HasAnyTag(List<string> tags)
        {
            _tags = tags;
        }


        internal override bool Matches(OsmElement element)
        {
            return
                element.RawElement.Tags != null &&
                element.RawElement.Tags.Any(t => _tags.Contains(t.Key));
        }
    }
}