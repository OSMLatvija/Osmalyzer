using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;

namespace Osmalyzer;

[UsedImplicitly]
public class VPVKACAnalyzer : Analyzer
{
    public override string Name => "VPVKAC Offices";

    public override string Description => "This report checks that VPVKAC (Valsts un pašvaldību vienotie klientu apkalpošanas centri) offices are mapped.";

    public override AnalyzerGroup Group => AnalyzerGroup.POIs;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData), typeof(VPVKACAnalysisData) ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract osmOffices = osmMasterData.Filter(
            new IsNode(),
            new HasValue("office", "government")
        );

        // Get office data

        VPVKACAnalysisData officeData = datas.OfType<VPVKACAnalysisData>().First();

        List<VPVKACOffice> offices = officeData.Offices;
        
        // Get office location from raw addresses

        List<LocatedVPVKACOffice> locatedOffices = [ ];
        
        List<VPVKACOffice> unlocatedOffices = [ ];

        foreach (VPVKACOffice office in offices)
        {
            LocatedVPVKACOffice? locateOffice = LocateOffice(office, osmData.MasterData);
            
            if (locateOffice != null)
                locatedOffices.Add(locateOffice);
            else
                unlocatedOffices.Add(office);
        }
            
        // Prepare data comparer/correlator

        Correlator<LocatedVPVKACOffice> correlator = new Correlator<LocatedVPVKACOffice>(
            osmOffices,
            locatedOffices,
            new MatchDistanceParamater(100),
            new MatchFarDistanceParamater(300),
            new MatchCallbackParameter<LocatedVPVKACOffice>(GetMatchStrength),
            new OsmElementPreviewValue("name", false),
            new DataItemLabelsParamater("VPVKAC office", "VPVKAC offices"),
            new LoneElementAllowanceParameter(DoesOsmElementLookLikeVPVKACOffice)
        );

        [Pure]
        MatchStrength GetMatchStrength(LocatedVPVKACOffice office, OsmElement osmElement)
        {
            string? name = osmElement.GetValue("name");

            if (name == office.Office.Name)
                return MatchStrength.Strong; // exact match on name

            if (DoesOsmElementLookLikeVPVKACOffice(osmElement))
                return MatchStrength.Good; // looks like a VPVKAC office, but not exact match
            
            return MatchStrength.Unmatched;
        }

        [Pure]
        bool DoesOsmElementLookLikeVPVKACOffice(OsmElement element)
        {
            string? name = element.GetValue("name");
            
            if (name != null && DoesNameLookLikeVPVKAC(name))
                return true; // looks like a VPVKAC office
            
            string? officialName = element.GetValue("official_name");
            
            if (officialName != null && DoesNameLookLikeVPVKAC(officialName))
                return true; // looks like a VPVKAC office

            return false;
            
            
            [Pure]
            bool DoesNameLookLikeVPVKAC(string value)
            {
                value = value.ToLower();
                
                return 
                    value.Contains("vpvkac") || 
                    value.Contains("valsts un pašvaldības vienotais klientu apkalpošanas centrs");
            }
        }

        // Parse and report primary matching and location correlation

        CorrelatorReport correlation = correlator.Parse(
            report,
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(true),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch()
        );

        // Report any offices we couldn't find an address for

        if (unlocatedOffices.Count > 0)
        {
            report.AddGroup(
                ExtraReportGroup.UnlocatedOffices,
                "Non-geolocated Offices",
                "These listed offices could not be geolocated to an OSM address. " +
                "Possibly, the data values are incorrect, differently-formatted or otherwise fail to match automatically."
            );
            
            foreach (VPVKACOffice unlocatedOffice in unlocatedOffices)
            {
                report.AddEntry(
                    ExtraReportGroup.UnlocatedOffices,
                    new IssueReportEntry(
                        "Office `" + unlocatedOffice.Name + "` could not be geolocated for `" + unlocatedOffice.Address.ToString(true) + "`"
                    )
                );
            }
        }
        
        // Validate additional issues

        // todo:

#if !REMOTE_EXECUTION
        // Export all offices
        
        List<IFeature> features = [ ];
        GeometryFactory geometryFactory = new GeometryFactory();

        foreach (UnmatchedItemCorrelation<LocatedVPVKACOffice> node in correlation.Correlations.OfType<UnmatchedItemCorrelation<LocatedVPVKACOffice>>())
        {
            Point? point = geometryFactory.CreatePoint(new Coordinate(node.DataItem.Coord.lon, node.DataItem.Coord.lat));
            AttributesTable attributes = new AttributesTable()
            {
                { "name", ShortenName(node.DataItem.Office.Name) },
                { "official_name", FullName(node.DataItem.Office.Name) },
                { "office", "government" },
                { "government", "public_service" }
            };

            [Pure]
            string FullName(string officeName)
            {
                // "Cēsu novada Vecpiebalgas pagasta VPVKAC" -> 'Cēsu novada Vecpiebalgas pagasta valsts un pašvaldības vienotais klientu apkalpošanas centrs"
                return officeName.Replace("VPVKAC", "valsts un pašvaldības vienotais klientu apkalpošanas centrs");
            }
            
            [Pure]
            string ShortenName(string officeName)
            {
                // "Cēsu novada Vecpiebalgas pagasta VPVKAC" -> 'Vecpiebalgas VPVKAC"
                return Regex.Replace(
                    officeName,
                    @"^(?:[A-Z][a-z]+) novada ([A-Z][a-z]+) pagasta VPVKAC",
                    @"$1 VPVKAC"
                );
            }

            features.Add(new Feature(point, attributes));
        }

        FeatureCollection featureCollection = new FeatureCollection(features);

        JsonSerializer? serializer = GeoJsonSerializer.Create();
        using StreamWriter writer = new StreamWriter("VPVKAC offices.geojson");
        serializer.Serialize(writer, featureCollection);
#endif
    }


    private static LocatedVPVKACOffice? LocateOffice(VPVKACOffice office, OsmMasterData osmData)
    {
        OsmCoord? coord = FuzzyAddressFinder.Find(
            osmData,
            office.Address.Name,
            office.Address.Location,
            office.Address.Pagasts,
            office.Address.Novads,
            office.Address.PostalCode
        );
        
        // todo: not using pagasts and novads, but are they ever ambiguous?
        
        if (coord == null)
            return null; // no location found

        return new LocatedVPVKACOffice(
            office,
            coord.Value
        );
    }


    private record LocatedVPVKACOffice(VPVKACOffice Office, OsmCoord Coord) : IDataItem
    {
        public string ReportString() => Office.ReportString();
    }
    
    
    private enum ExtraReportGroup
    {
        UnlocatedOffices
    }
}