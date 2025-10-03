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
            string? official_name = osmElement.GetValue("official_name");

            if (official_name == office.Office.Name)
                return MatchStrength.Strong; // exact match on name
            
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
                        '`' + locatedOffice.Office.ShortName + "` office at `" +
                        locatedOffice.Office.Address.ToString(true) +
                        "` can be added as" + Environment.NewLine + tagsBlock,
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
            report.AddGroup(
                ExtraReportGroup.SuggestedUpdates,
                "Suggested Updates",
                "These matched VPVKAC offices have missing or mismatched tags compared to parsed source data. " +
                "Note that source data is not guaranteed to be correct and parsing is not guaranteed to have correct OSM values."
            );

            foreach (MatchedCorrelation<LocatedVPVKACOffice> pair in matchedPairs)
            {
                OsmElement osmOffice = pair.OsmElement;
                LocatedVPVKACOffice office = pair.DataItem;

                // Expected values
                string expectedName = office.Office.ShortName;
                string expectedOfficialName = FullName(office.Office.Name);
                string? expectedEmail = string.IsNullOrWhiteSpace(office.Office.Email) ? null : office.Office.Email;
                string? expectedPhone = string.IsNullOrWhiteSpace(office.Office.Phone) ? null : office.Office.Phone;
                string? expectedOpeningHours = string.IsNullOrWhiteSpace(office.Office.OpeningHours) ? null : office.Office.OpeningHours;

                // `name`
                if (!string.IsNullOrWhiteSpace(expectedName))
                {
                    string? actual = osmOffice.GetValue("name");
                    if (actual == null)
                        AddMissing("name", expectedName);
                    else if (actual != expectedName)
                        AddDifferent("name", actual, expectedName);
                }

                // `official_name`
                if (!string.IsNullOrWhiteSpace(expectedOfficialName))
                {
                    string? actual = osmOffice.GetValue("official_name");
                    if (actual == null)
                        AddMissing("official_name", expectedOfficialName);
                    else if (actual != expectedOfficialName)
                        AddDifferent("official_name", actual, expectedOfficialName);
                }

                // `office`
                {
                    string? actual = osmOffice.GetValue("office");
                    if (actual == null)
                        AddMissing("office", "government");
                    else if (actual != "government")
                        AddDifferent("office", actual, "government");
                }

                // `government`
                {
                    string? actual = osmOffice.GetValue("government");
                    if (actual == null)
                        AddMissing("government", "public_service");
                    else if (actual != "public_service")
                        AddDifferent("government", actual, "public_service");
                }

                // `email`
                if (expectedEmail != null)
                {
                    string? actual = osmOffice.GetValue("email");
                    if (actual == null)
                        AddMissing("email", expectedEmail);
                    else if (actual != expectedEmail)
                        AddDifferent("email", actual, expectedEmail);
                }

                // `phone`
                if (expectedPhone != null)
                {
                    string? actual = osmOffice.GetValue("phone");
                    if (actual == null)
                        AddMissing("phone", expectedPhone);
                    else if (actual != expectedPhone)
                        AddDifferent("phone", actual, expectedPhone);
                }

                // `opening_hours`
                if (expectedOpeningHours != null)
                {
                    string? actual = osmOffice.GetValue("opening_hours");
                    if (actual == null)
                        AddMissing("opening_hours", expectedOpeningHours);
                    else if (actual != expectedOpeningHours)
                        AddDifferent("opening_hours", actual, expectedOpeningHours);
                }
                
                continue;
                

                void AddMissing(string tag, string expected)
                {
                    report.AddEntry(
                        ExtraReportGroup.SuggestedUpdates,
                        new IssueReportEntry(
                            "`" + office.Office.ShortName + "` office " +
                            "is missing `" + tag + "=" + expected + "` - " + 
                            osmOffice.OsmViewUrl,
                            osmOffice.AverageCoord,
                            MapPointStyle.Problem,
                            osmOffice
                        )
                    );
                }

                void AddDifferent(string tag, string actual, string expected)
                {
                    report.AddEntry(
                        ExtraReportGroup.SuggestedUpdates,
                        new IssueReportEntry(
                            "`" + office.Office.ShortName + "` office " +
                            "has `" + tag + "=" + actual + "` " +
                            "but expecting `" + tag + "=" + expected + "` - " + 
                            osmOffice.OsmViewUrl,
                            osmOffice.AverageCoord,
                            MapPointStyle.Problem,
                            osmOffice
                        )
                    );
                }
            }
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
                { "name", node.Office.ShortName },
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
        
        string shortName = office.ShortName;
        string fullName = FullName(office.Name);
        
        if (!string.IsNullOrWhiteSpace(shortName)) lines.Add("name=" + shortName);
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
        SuggestedAdditions,
        SuggestedUpdates
    }
}