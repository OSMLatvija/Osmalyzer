using JetBrains.Annotations;

namespace Osmalyzer
{
    public interface IQuickComparerDataItem
    {
        public OsmCoord Coord { get; }


        [Pure]
        public string ReportString();
    }
}