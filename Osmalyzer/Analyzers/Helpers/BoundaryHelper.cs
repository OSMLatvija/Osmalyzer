using JetBrains.Annotations;

namespace Osmalyzer
{
    public static class BoundaryHelper
    {
        // todo: cache
        
        [Pure]
        public static OsmPolygon GetRigaPolygon(OsmMasterData osmData)
        {
            return GetAdminRelationPolygon(osmData, "6", "Rīga");
        }

        [Pure]
        public static OsmPolygon GetLatviaPolygon(OsmMasterData osmData)
        {
            return GetAdminRelationPolygon(osmData, "2", "Latvija");
        }

        
        [Pure]
        private static OsmPolygon GetAdminRelationPolygon(OsmMasterData osmData, string level, string name)
        {
            OsmRelation rigaRelation = (OsmRelation)osmData.Find(
                new IsRelation(),
                new HasValue("type", "boundary"),
                new HasValue("admin_level", level),
                new HasValue("name", name)
            )!; // never expecting to not have this

            return rigaRelation.GetOuterWayPolygon();
        }
    }
}