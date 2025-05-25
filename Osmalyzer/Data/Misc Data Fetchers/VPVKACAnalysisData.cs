using System.Web;

namespace Osmalyzer;

[UsedImplicitly]
public class VPVKACAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public override string Name => "VPVKAC";

    public override string ReportWebLink => @"https://www.pakalpojumucentri.lv/vpvkac";

    public override bool NeedsPreparation => true;


    protected override string DataFileIdentifier => "vpvkac";


    public List<VPVKACOffice> Offices { get; private set; } = null!; // only null before prepared


    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            "https://www.pakalpojumucentri.lv/vpvkac", 
            Path.Combine(CacheBasePath, DataFileIdentifier + @".html")
        );
    }

    protected override void DoPrepare()
    {
        Offices = [ ];

        string text = File.ReadAllText(Path.Combine(CacheBasePath, DataFileIdentifier + @".html"));
        
        /*
                                                                    <tr>
                                        <td>
                                            <span style="color: #999999;">95)</span>

                                            <a href="https://www.pakalpojumucentri.lv/vpvkac/grobinas-novads"><strong>
                                                        Dienvidkurzemes novada Grobiņas pilsētas VPVKAC
                                                    </strong></a>
                                                <span style="color: #999999;"><br />Novadu nozīmes attīstības centrs</span>

                                            
                                                <br /><br />
                                            <a href="https://www.pakalpojumucentri.lv/vpvkac/grobinas-novads">Lielā iela 54<br /> Grobiņas pilsēta<br /> Dienvidkurzemes nov.<br /> LV-3430</a>
                                        </td>
                                        <td class="text-left">Pirmdiena: 8:30 - 12:00; 12:20 - 17:00<br />
Otrdiena: 8:30 - 12:00; 12:30 - 19:00<br />
Trešdiena: 8:30 - 12:00; 12:20 - 17:00<br />
Ceturtdiena: 8:30 - 12:00; 12:20 - 17:00<br />
Piektdiena: 8:30 - 14:00</td>
                                        <td class="text-right">grobina@pakalpojumucentri.lv<br /><i class="fa fa-phone" aria-hidden="true"></i>&nbsp;&nbsp;66954818</td>
                                                                                                                    </tr>
                            
        */            

        MatchCollection rowMatches = Regex.Matches(text, @"<tr>(.*?)<\/tr>", RegexOptions.Singleline);

        if (rowMatches.Count == 0) throw new Exception();

        foreach (Match rowMatch in rowMatches)
        {
            string rowText = rowMatch.Groups[1].ToString();
            
            Match nameMatch = Regex.Match(rowText, @"<strong>([^<]+?)<\/strong>");
            if (!nameMatch.Success)
                continue;
            
            if (rowText.Contains("Tiks atvērts"))
                continue; // this is a future office

            string name = nameMatch.Groups[1].ToString().Trim();
            // e.g. "Dienvidkurzemes novada Grobiņas pilsētas VPVKAC"

            name = HttpUtility.HtmlDecode(name); // e.g. "Aug&scaron;daugavas novada Vi&scaron;ķu pagasta VPVKAC"

            MatchCollection addressMatches = Regex.Matches(rowText, @"<a href=""[^""]+"">(.*?)</a>", RegexOptions.Singleline);
            // This will match both <a> - name and address
            if (addressMatches.Count != 2) throw new Exception();

            VPVKACOffice.VPVKACAddress? address = CleanAddress(addressMatches[1].Groups[1].ToString().Trim());
            if (address == null) throw new Exception();

            // todo: opening hours
            // todo: email
            // todo: phone
            
            Offices.Add(new VPVKACOffice(name, address));
        }
        
        if (Offices.Count == 0) throw new Exception();
    }

    
    [Pure]
    private static VPVKACOffice.VPVKACAddress? CleanAddress(string raw)
    {
        // e.g. "Lielā iela 54<br /> Grobiņas pilsēta<br /> Dienvidkurzemes nov.<br /> LV-3430"
        
        string[] parts = raw.Split("<br />", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 3)
        {
            // Brīvības gatve 455
            // Rīga
            // LV-1024 
            
            string name = parts[0]; // e.g. "Brīvības gatve 455"
            string location = parts[1]; // e.g. "Rīga"
            string postalCode = parts[2]; // e.g. "LV-1024"

            if (Regex.IsMatch(postalCode, @"^LV-\d{4}$") == false) return null;

            name = TryCleanName(name);
            location = TryCleanCityName(location);
            
            return new VPVKACOffice.VPVKACAddress(
                name,
                location,
                null, // pagasts
                null, // novads
                postalCode
            );
        }
        else if (parts.Length == 4)
        {
            // Atmodas iela 22
            // Aizputes pilsēta
            // Dienvidkurzemes nov.
            // LV-3456   
            
            string name = parts[0]; // e.g. "Atmodas iela 22"
            string location = parts[1]; // e.g. "Aizputes pilsēta"
            string novads = parts[2]; // e.g. "Dienvidkurzemes nov."
            string postalCode = parts[3]; // e.g. "LV-3456"

            if (!novads.Contains("nov.")) return null;
            if (Regex.IsMatch(postalCode, @"^LV-\d{4}$") == false) return null;

            name = TryCleanName(name);
            location = TryCleanCityName(location);
            novads = TryCleanNovads(novads);
            
            return new VPVKACOffice.VPVKACAddress(
                name,
                location,
                null, // pagasts
                novads,
                postalCode
            );
        }
        else if (parts.Length == 5)
        {
            if (parts[2].Contains("pilsēta")) // special case
            {     
                // Gaismas iela 19
                // k.9-1
                // Ķekavas pilsēta
                // Ķekavas nov.
                // LV-2123  
                
                string name = parts[0]; // e.g. "Gaismas iela 19"
                // Ignoring e.g. "k.9-1"
                string location = parts[2]; // e.g. "Ķekavas pilsēta"
                string novads = parts[3]; // e.g. "Ķekavas nov."
                string postalCode = parts[4]; // e.g. "LV-2123"

                if (!novads.Contains("nov.")) return null;
                if (!Regex.IsMatch(postalCode, @"^LV-\d{4}$")) return null;

                name = TryCleanName(name);
                novads = TryCleanNovads(novads);

                return new VPVKACOffice.VPVKACAddress(
                    name,
                    location,
                    null,
                    novads,
                    postalCode
                );
            }
            else
            {       
                // Alauksta iela 4
                // Vecpiebalga
                // Vecpiebalgas pag.
                // Cēsu nov.
                // LV-4122   
                
                string name = parts[0]; // e.g. "Alauksta iela 4"
                string location = parts[1]; // e.g. "Vecpiebalga"
                string pagasts = parts[2]; // e.g. "Vecpiebalgas pag."
                string novads = parts[3]; // e.g. "Cēsu nov."
                string postalCode = parts[4]; // e.g. "LV-4122"

                if (!pagasts.Contains("pag.")) return null;
                if (!novads.Contains("nov.")) return null;
                if (!Regex.IsMatch(postalCode, @"^LV-\d{4}$")) return null;

                name = TryCleanName(name);
                location = TryCleanCityName(location);
                pagasts = TryCleanPagasts(pagasts);
                novads = TryCleanNovads(novads);

                return new VPVKACOffice.VPVKACAddress(
                    name,
                    location,
                    pagasts,
                    novads,
                    postalCode
                );
            }
        }
        else 
        {
            return null;
        }
    }

    
    [Pure]
    private static string TryCleanName(string name)
    {
        name = name.Replace('“', '"').Replace('”', '"'); // e.g. "“Silavas”", just use normal quotes that address matcher understands

        name = name.Replace("  ", " "); // e.g. "Raiņa  iela"
        
        name = name.Replace("ielā", "iela"); // e.g. "Skolas ielā 17" or "Bērzu ielā"
        
        name = name.Replace("Somersetas", "Somersētas"); // unique typo
        name = name.Replace("Skola iela", "Skolas iela"); // unique typo
        name = name.Replace("Brinģenes", "Briģenes"); // unique typo
        name = name.Replace("Liela", "Lielā"); // unique typo
        
        name = name.Replace("Skolas iela 4-40", "Skolas iela 4"); // 40 is the flat number in an apartment building
        name = name.Replace("Pils iela 5-1", "Pils iela 5"); // 1 must be some in-building designation
        
        return name;
    }

    [Pure]
    private static string TryCleanCityName(string location)
    {
        if (location.Contains("pilsēta"))
            location = location
                       .Replace("s pilsēta", "") // generic e.g. "Grobiņas pilsēta" -> "Grobiņa", many matches like this
                       .Replace("u pilsēta", "i") // generic e.g. "Ādažu pilsēta" -> "Ādaži", multiple matches like this
                       .Replace("Pļaviņu pilsēta", "Pļaviņas") // special case "Pļaviņu pilsēta" -> "Pļaviņas"
                       .Replace("Cēsu pilsēta", "Cēsis"); // special case "Cēsu pilsēta" -> "Cēsis"

        return location;
    }

    [Pure]
    private static string TryCleanPagasts(string pagasts)
    {
        return pagasts.Replace("pag.", "pagasts").Trim();
    }

    [Pure]
    private static string TryCleanNovads(string novads)
    {
        return novads.Replace("nov.", "novads").Trim();
    }
}