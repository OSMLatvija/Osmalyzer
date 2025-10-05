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
            LocatedVPVKACOffice? locateOffice = TryLocateOffice(office, osmData.MasterData);
            
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
            string? official_name = osmElement.GetValue("official_name");

            if (official_name == office.Office.Name)
                return MatchStrength.Strong; // exact match on official name
            
            string? name = osmElement.GetValue("name");

            if (name == office.Office.Name)
                return MatchStrength.Strong; // exact match on full name

            if (name == office.Office.DisplayName)
                return MatchStrength.Strong; // exact match on disambiguated display name

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
        
        // Offer syntax for quick OSM addition for unmatched located offices
        
        List<LocatedVPVKACOffice> unmatchedLocatedOffices = correlation.Correlations
            .OfType<UnmatchedItemCorrelation<LocatedVPVKACOffice>>()
            .Select(c => c.DataItem)
            .ToList();

        if (unmatchedLocatedOffices.Count > 0)
        {
            report.AddGroup(
                ExtraReportGroup.SuggestedAdditions,
                "Suggested Additions",
                "These VPVKAC offices are not currently matched to OSM and can be added with these (suggested) tags. " +
                "(Address fields are not offered because the local bot will update these automatically on OSM.)"
            );

            foreach (LocatedVPVKACOffice locatedOffice in unmatchedLocatedOffices)
            {
                string tagsBlock = BuildSuggestedTags(locatedOffice.Office);

                report.AddEntry(
                    ExtraReportGroup.SuggestedAdditions,
                    new IssueReportEntry(
                        '`' + locatedOffice.Office.DisplayName + "` office at `" +
                        locatedOffice.Office.Address.ToString(true) +
                        "` can be added at " +
                        locatedOffice.Coord.OsmUrl +
                        " as" + Environment.NewLine + tagsBlock,
                        locatedOffice.Coord,
                        MapPointStyle.CorrelatorItemUnmatched
                    )
                );
            }
        }
        
        // Offer updates to matched office values
        
        List<MatchedCorrelation<LocatedVPVKACOffice>> matchedPairs = correlation.Correlations
            .OfType<MatchedCorrelation<LocatedVPVKACOffice>>()
            .ToList();

        if (matchedPairs.Count > 0)
        {
            List<TagComparison<LocatedVPVKACOffice>> comparisons = [
                new TagComparison<LocatedVPVKACOffice>(
                    "name",
                    d => string.IsNullOrWhiteSpace(d.Office.DisplayName) ? d.Office.Name : d.Office.DisplayName
                ),
                new TagComparison<LocatedVPVKACOffice>(
                    "official_name",
                    d => string.IsNullOrWhiteSpace(d.Office.Name) ? null : FullName(d.Office.Name)
                ),
                new TagComparison<LocatedVPVKACOffice>(
                    "office",
                    _ => "government"
                ),
                new TagComparison<LocatedVPVKACOffice>(
                    "government",
                    _ => "public_service"
                ),
                new TagComparison<LocatedVPVKACOffice>(
                    "email",
                    d => string.IsNullOrWhiteSpace(d.Office.Email) ? null : d.Office.Email
                ),
                new TagComparison<LocatedVPVKACOffice>(
                    "phone",
                    d => string.IsNullOrWhiteSpace(d.Office.Phone) ? null : d.Office.Phone
                ),
                new TagComparison<LocatedVPVKACOffice>(
                    "opening_hours",
                    d => string.IsNullOrWhiteSpace(d.Office.OpeningHours) ? null : d.Office.OpeningHours
                )
            ];

            TagSuggester<LocatedVPVKACOffice> suggester = new TagSuggester<LocatedVPVKACOffice>(
                matchedPairs,
                d => d.Office.DisplayName,
                "office"
            );

            suggester.Suggest(
                report,
                comparisons
            );
        }
        
        
        // Validate additional issues
        // todo: like what?

#if !REMOTE_EXECUTION
        // Export all offices (unmatched) to GeoJSON for local runs
        
        List<IFeature> features = [ ];
        GeometryFactory geometryFactory = new GeometryFactory();

        foreach (LocatedVPVKACOffice node in correlation.Correlations.OfType<UnmatchedItemCorrelation<LocatedVPVKACOffice>>().Select(n => n.DataItem))
        //foreach (LocatedVPVKACOffice node in locatedOffices)
        {
            Point? point = geometryFactory.CreatePoint(new Coordinate(node.Coord.lon, node.Coord.lat));
            AttributesTable attributes = new AttributesTable()
            {
                { "name", node.Office.DisplayName },
                { "official_name", FullName(node.Office.Name) },
                { "office", "government" },
                { "government", "public_service" },
                { "email", node.Office.Email },
                { "phone", node.Office.Phone },
                { "opening_hours", node.Office.OpeningHours },
                { "__address", node.Office.Address.ToString(false) }, // for debug
            };

            features.Add(new Feature(point, attributes));
        }

        FeatureCollection featureCollection = new FeatureCollection(features);

        JsonSerializer? serializer = GeoJsonSerializer.Create();
        using StreamWriter writer = new StreamWriter("VPVKAC offices.geojson");
        serializer.Serialize(writer, featureCollection);
#endif
    }


    private static LocatedVPVKACOffice? TryLocateOffice(VPVKACOffice office, OsmMasterData osmData)
    {
        OsmCoord? coord = FuzzyAddressFinder.Find(
            osmData,
            office.Address.Name,
            office.Address.Location,
            office.Address.Pagasts,
            office.Address.Novads,
            office.Address.PostalCode
        );
        
        // todo: not using "pagasts" and "novads", but are they ever ambiguous?
        
        if (coord == null)
            return null; // no location found

        return new LocatedVPVKACOffice(
            office,
            coord.Value
        );
    }

    [Pure]
    private static string BuildSuggestedTags(VPVKACOffice office)
    {
        List<string> lines = [ ];
        
        string displayName = office.DisplayName; // disambiguated name, if applicable
        string fullName = FullName(office.Name);
        
        if (!string.IsNullOrWhiteSpace(displayName)) lines.Add("name=" + displayName);
        if (!string.IsNullOrWhiteSpace(fullName)) lines.Add("official_name=" + fullName);
        lines.Add("office=government");
        lines.Add("government=public_service");
        if (!string.IsNullOrWhiteSpace(office.Email)) lines.Add("email=" + office.Email);
        if (!string.IsNullOrWhiteSpace(office.Phone)) lines.Add("phone=" + office.Phone);
        if (!string.IsNullOrWhiteSpace(office.OpeningHours)) lines.Add("opening_hours=" + office.OpeningHours);
        
        return "```" + string.Join(Environment.NewLine, lines) + "```";
    }

    [Pure]
    private static string FullName(string officeName)
    {
        // "Cēsu novada Vecpiebalgas pagasta VPVKAC" -> "Cēsu novada Vecpiebalgas pagasta valsts un pašvaldības vienotais klientu apkalpošanas centrs"
        return officeName.Replace("VPVKAC", "valsts un pašvaldības vienotais klientu apkalpošanas centrs");
    }

    private record LocatedVPVKACOffice(VPVKACOffice Office, OsmCoord Coord) : IDataItem
    {
        public string ReportString() => Office.ReportString();
    }
    
    
    private enum ExtraReportGroup
    {
        UnlocatedOffices,
        SuggestedAdditions
    }
}