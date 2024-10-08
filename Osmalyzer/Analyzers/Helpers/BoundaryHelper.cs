﻿namespace Osmalyzer;

public static class BoundaryHelper
{
    private static OsmPolygon? _latviaPolygon;

    private static OsmPolygon? _rigaPolygon;

    private static OsmPolygon? _daugavpilsPolygon;


    [Pure]
    public static OsmPolygon GetLatviaPolygon(OsmMasterData osmData)
    {
        if (_latviaPolygon == null)
            _latviaPolygon = GetAdminRelationPolygon(osmData, "2", "Latvija");

        return _latviaPolygon;
    }

    [Pure]
    public static OsmPolygon GetRigaPolygon(OsmMasterData osmData)
    {
        if (_rigaPolygon == null)
            _rigaPolygon = GetAdminRelationPolygon(osmData, "5", "Rīga");
        
        return _rigaPolygon;
    }

    [Pure]
    public static OsmPolygon GetDaugavpilsPolygon(OsmMasterData osmData)
    {
        if (_daugavpilsPolygon == null)
            _daugavpilsPolygon = GetAdminRelationPolygon(osmData, "5", "Daugavpils");
        
        return _daugavpilsPolygon;
    }

    [Pure]
    private static OsmPolygon GetAdminRelationPolygon(OsmMasterData osmData, string level, string name)
    {
        OsmRelation relation = (OsmRelation)osmData.Find(
            new IsRelation(),
            new HasValue("type", "boundary"),
            new HasValue("admin_level", level),
            new HasValue("name", name)
        )!; // never expecting to not have this

        return relation.GetOuterWayPolygon();
    }
}