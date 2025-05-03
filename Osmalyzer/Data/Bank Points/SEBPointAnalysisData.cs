namespace Osmalyzer;

[UsedImplicitly]
public class SEBPointAnalysisData : BankPointAnalysisData
{
    public override string Name => "SEB Points";

    public override string ReportWebLink => @"https://www.seb.lv/atm-find";

    public override bool NeedsPreparation => true;
    

    protected override string DataFileIdentifier => "seb-points";
    

    protected override void Download()
    {
        // Storing files in sub-folder because we have many
        if (!Directory.Exists(Path.Combine(CacheBasePath, DataFileIdentifier)))
            Directory.CreateDirectory(Path.Combine(CacheBasePath, DataFileIdentifier));
        
        int atmPageCount = 1;
        
        for (int i = 0; i < atmPageCount; i++)
        {
            string pageContent = WebsiteBrowsingHelper.Read(
                @"https://www.seb.lv/atm-find?page=" + i, 
                true,
                null,
                new WaitForElementOfClass("pager__item") // website has internal delayed data loading shenanigans 
            );
        
            // When getting pages, parse it for the total page count from the paginator (which grows with more pages)
            MatchCollection matches = Regex.Matches(pageContent, @"<a href=""\?page=(\d+)""");
            atmPageCount = matches.Select(m => int.Parse(m.Groups[1].ToString())).Max() + 1;

            File.WriteAllText(
                Path.Combine(CacheBasePath, DataFileIdentifier, @"A" + i + @".html"),
                pageContent
            );
        }      

        if (atmPageCount == 0)
            throw new Exception("Failed to download ATM pages");  
        
        int branchPageCount = 1;

        for (int i = 0; i < branchPageCount; i++)
        {
            string pageContent = WebsiteBrowsingHelper.Read(
                @"https://www.seb.lv/atm-find?type_id=2&page=" + i, 
                true,
                null,
                new WaitForElementOfClass("pager__item") // website has internal delayed data loading shenanigans 
            );

            // When getting pages, parse it for the total page count from the paginator (which grows with more pages)
            MatchCollection matches = Regex.Matches(pageContent, @"<a href=""\?type_id=2&amp;page=(\d+)""");
            branchPageCount = matches.Select(m => int.Parse(m.Groups[1].ToString())).Max() + 1;

            File.WriteAllText(
                Path.Combine(CacheBasePath, DataFileIdentifier, @"B" + i + @".html"),
                pageContent
            );
        }

        if (branchPageCount == 0)
            throw new Exception("Failed to download branch pages");
    }

    protected override void DoPrepare()
    {
        Points = new List<BankPoint>();

        int atmPageCount = GetPageCount("A");
        
        for (int i = 0; i < atmPageCount; i++)
        {
            string pageData = File.ReadAllText(Path.Combine(CacheBasePath, DataFileIdentifier, @"A" + i + @".html"));

            // <div><div class="row mx-0 border-top border-secondary pt-4 pb-lg-3 c-atm-search__item">
            //
            //   <div class="col-md-6">
            //     <h5 class="mb-2">Kraslava</h5>
            //           <div>Izmaksas bankomāts</div>
            //     
            //     <div class="mb-2"><div class="office-hours office-hours-status--open">
            //       <div class="office-hours__item">
            //               <span class="office-hours__item-label" style="width: 3.6em;">P.-Sv.</span>
            //                     <span class="office-hours__item-slots">24H</span>
            //                   <span><br /></span>
            //     </div>
            //   </div>
            // </div>
            //   </div>
            //
            //   <div class="col-md-6">
            //     <div class="mb-2">Baznīcas iela 1, Krāslava</div>
            //     <div class="d-flex flex-wrap mb-md-2 mr-n4">
            //       <a href="https://www.google.com/maps/search/?api=1&amp;query=55.897472,27.171056" class="mr-4 mb-md-1 text-nowrap" rel="noopener nofollow" target="_blank">Apskatīt kartē</a>
            //       <a href="https://www.google.com/maps/dir/?api=1&amp;origin=&destination=55.897472,27.171056" class="mr-4 mb-md-1 text-nowrap" rel="noopener nofollow" target="_blank">Kā nokļūt</a>
            //     </div>
            //   </div>
            //
            // </div>
            // </div>

            MatchCollection entryMatches = Regex.Matches(pageData, @"(<h5.*?<a href=""https:\/\/www\.google\.com\/maps\/dir)", RegexOptions.Singleline);

            foreach (Match entryMatch in entryMatches)
            {
                string entryContent = entryMatch.Groups[1].ToString();

                string title = Regex.Match(entryContent, @"<h5 class=""mb-2"">([^<]+)<\/h5>").Groups[1].ToString();
                string address = Regex.Match(entryContent, @"<div class=""mb-2"">([^<]+)</div>").Groups[1].ToString();
                string typeRaw = Regex.Match(entryContent, @"</h5>\s+<div>([^<]+)<\/div>").Groups[1].ToString(); // after H5
                Match coordMatch = Regex.Match(entryContent, @"query=([^,]+),([^""]+)");
                double lat = double.Parse(coordMatch.Groups[1].ToString());
                double lon = double.Parse(coordMatch.Groups[2].ToString());
                OsmCoord coord = new OsmCoord(lat, lon);

                BankPointType _ = RawTypeToPointType(typeRaw, out bool? deposit);

                BankPoint point = new BankAtmPoint("SEB", title, address, coord, deposit);

                Points.Add(point);
            }
        }

        int branchPageCount = GetPageCount("B");

        for (int i = 0; i < branchPageCount; i++)
        {
            string pageData = File.ReadAllText(Path.Combine(CacheBasePath, DataFileIdentifier, @"B" + i + @".html"));
            
            //   <div><div class="row mx-0 border-top border-secondary pt-4 pb-lg-3 c-atm-search__item">
            //
            // <div class="col-md-6">            
            //     <h5 class="mb-2">Cēsis</h5>
            //           <div></div>
            //       <div></div>
            //     
            //     <div class="mb-2"><div class="office-hours office-hours-status--closed">
            //       <div class="office-hours__item">
            //               <span class="office-hours__item-label" style="width: 3.6em;">P.-Tr.</span>
            //                     <span class="office-hours__item-slots">09:00-17:00</span>
            //                   <span><br /></span>
            //     </div>
            //       <div class="office-hours__item">
            //               <span class="office-hours__item-label" style="width: 3.6em;">Ce.</span>
            //                     <span class="office-hours__item-slots">08:30-18:30</span>
            //                   <span><br /></span>
            //     </div>
            //       <div class="office-hours__item">
            //               <span class="office-hours__item-label" style="width: 3.6em;">Pk.</span>
            //                     <span class="office-hours__item-slots">09:00-15:00</span>
            //                   <span><br /></span>
            //     </div>
            //   </div>
            // </div>
            //   </div>
            //
            //   <div class="col-md-6">
            //     <div class="mb-2">Pils iela 4, Cēsis, Cēsu novads, LV 4101, Latvija, Cēsis</div>
            //     <div class="d-flex flex-wrap mb-md-2 mr-n4">
            //       <a href="https://www.google.com/maps/search/?api=1&amp;query=57.313008,25.273736" class="mr-4 mb-md-1 text-nowrap" rel="noopener nofollow" target="_blank">Apskatīt kartē</a>
            //       <a href="https://www.google.com/maps/dir/?api=1&amp;origin=&destination=57.313008,25.273736" class="mr-4 mb-md-1 text-nowrap" rel="noopener nofollow" target="_blank">Kā nokļūt</a>
            //     </div>
            //   </div>
            //
            // </div>
            // </div>

            MatchCollection entryMatches = Regex.Matches(pageData, @"(<h5.*?<a href=""https:\/\/www\.google\.com\/maps\/dir)", RegexOptions.Singleline);
            
            foreach (Match entryMatch in entryMatches)
            {
                string entryContent = entryMatch.Groups[1].ToString();
                
                string title = Regex.Match(entryContent, @"<h5 class=""mb-2"">([^<]+)<\/h5>").Groups[1].ToString();
                string address = Regex.Match(entryContent, @"<div class=""mb-2"">([^<]+)</div>").Groups[1].ToString();
                Match coordMatch = Regex.Match(entryContent, @"query=([^,]+),([^""]+)");
                double lat = double.Parse(coordMatch.Groups[1].ToString());
                double lon = double.Parse(coordMatch.Groups[2].ToString());
                OsmCoord coord = new OsmCoord(lat, lon);

                BankPoint point = new BankBranchPoint("SEB", title, address, coord);
                
                Points.Add(point);
            }
        }
    }

    [Pure]
    private int GetPageCount(string prefix)
    {
        // Stuff/A1.html
        // Stuff/A10.html
        // Stuff/A11.html
        // Stuff/A2.html
        // ..
        // Stuff/A9.html
        // to
        // 11

        string[] dataFiles = Directory.GetFiles(Path.Combine(CacheBasePath, DataFileIdentifier), "*.html");

        if (dataFiles.Length == 0)
            throw new Exception("Missing all data files!");

        List<string> relevantDataFiles =
            dataFiles
                .Select(Path.GetFileNameWithoutExtension)
                .Where(f => f!.StartsWith(prefix))
                .ToList()!;
        
        if (relevantDataFiles.Count == 0)
            throw new Exception("Missing relevant data files!");
        
        return relevantDataFiles
               .Select(f => int.Parse(f[1..]))
               .Max();
    }


    [Pure]
    private static BankPointType RawTypeToPointType(string rawType, out bool? deposit)
    {
        switch (rawType)
        {
            case "Izmaksas bankomāts":
                deposit = false;
                return BankPointType.Atm;
            
            case "Iemaksas un izmaksas bankomāts":
                deposit = true;
                return BankPointType.Atm;
            
            default:
                throw new NotImplementedException();
        }
    }

    private enum BankPointType
    {
        Atm
    }
}