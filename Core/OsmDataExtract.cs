using System;
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


        internal OsmDataExtract(OsmMasterData data, List<OsmElement> elements)
        {
            FullData = data;

            CreateElements(null, null, null, null);

            foreach (OsmElement element in elements)
                AddElement(element);
        }
    }
}