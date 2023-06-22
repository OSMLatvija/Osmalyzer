using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using OsmSharp;

namespace Osmalyzer
{
    public class OsmRelation : OsmElement
    {
        [PublicAPI]
        public IReadOnlyList<OsmRelationMember> Members => members.AsReadOnly();

        /// <summary>
        ///
        /// This will not contain null/missing elements, even if some are not loaded.
        /// </summary>
        [PublicAPI]
        public IEnumerable<OsmElement> Elements => members.Where(m => m.Element != null).Select(m => m.Element)!;
        
        
        internal readonly List<OsmRelationMember> members;


        internal OsmRelation(OsmGeo RawElement)
            : base(RawElement)
        {
            members = ((Relation)RawElement).Members.Select(m => new OsmRelationMember(this, m.Type, m.Id, m.Role)).ToList();
        }
    }
}