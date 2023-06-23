using System.Collections.Generic;
using JetBrains.Annotations;

namespace Osmalyzer
{
    /// <summary>
    /// A partial (filtered or trimmed) list of OSM data coming from a <see cref="OsmMasterData"/>.
    /// </summary>
    public class OsmDataExtract : OsmData
    {
        [PublicAPI]
        public OsmMasterData FullData { get; }

        [PublicAPI]
        public override IReadOnlyList<OsmElement> Elements => _elements.AsReadOnly();

        
        private readonly List<OsmElement> _elements = new List<OsmElement>();
        

        internal OsmDataExtract(OsmMasterData data, params OsmFilter[] filters)
        {
            FullData = data;

            foreach (OsmElement element in data.Elements)
                if (OsmElementMatchesFilters(element, filters))
                    _elements.Add(element);
        }

        internal OsmDataExtract(OsmMasterData data, List<OsmElement> elements)
        {
            FullData = data;

            _elements = elements;
        }
    }
}