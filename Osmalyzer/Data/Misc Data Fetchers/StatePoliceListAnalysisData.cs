using System.Net;

namespace Osmalyzer;

[UsedImplicitly]
public class StatePoliceListAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public override string Name => "StatePolice List";

    public override string ReportWebLink => @"https://www.vp.gov.lv/lv/filiales";

    public override bool NeedsPreparation => true;


    protected override string DataFileIdentifier => "state-police-list";

    private string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".html");


    public List<StatePoliceListEntry> Entries { get; private set; } = null!; // only null before prepared


    protected override void Download()
    {
        WebsiteBrowsingHelper.DownloadPage(
            ReportWebLink,
            DataFileName,
            true,
            "branch-row" // ensure the branch list is in the page
        );
    }

    protected override void DoPrepare()
    {
        Entries = [ ];

        string source = File.ReadAllText(DataFileName);

        // Each branch is wrapped in:
        // <div role="article" class="node small-content-block bg-white rounded branch-row">
        //   <div class="left-side">
        //     <a href="/lv/filiale/..." rel="bookmark">
        //       <h3>Valsts policijas ... iecirknis ...</h3>
        //     </a>
        //     <div class="branch-teaser-contacts">
        //       [optional] <div class="branch_contacts__branch__new-phone">
        //                    <div><div class="field__item field-phone">
        //                      <a href="tel:+371 NNNNN" ...>+371 NNNNN</a>
        //                    </div></div>
        //                  </div>
        //       <div class="branch_contacts__new-short-branch-numbe">  -- always 112, skip
        //         ...
        //       </div>
        //       <div class="field-email">
        //         <span class="visually-hidden">E-pasts: </span>
        //         <span class="spamspan"><span class="u">USER</span> [at] <span class="d">DOMAIN</span></span>
        //       </div>
        //     </div>
        //   </div>
        // </div>

        MatchCollection branchMatches = Regex.Matches(
            source,
            @"<div role=""article"" class=""node small-content-block bg-white rounded branch-row"">(.*?)</div><!-- /\.node -->",
            RegexOptions.Singleline
        );

        if (branchMatches.Count == 0)
            throw new Exception("Did not match any branch entries in state police list page");

        foreach (Match branchMatch in branchMatches)
        {
            string block = branchMatch.Groups[1].Value;

            string name = ParseName(block);
            string? phone = ParsePhone(block);
            string? email = ParseEmail(block);

            if (email == null)
                throw new Exception(); // not expecting current data to have any entries without email

            Entries.Add(new StatePoliceListEntry(name, phone, email));
        }
    }

    
    private static string ParseName(string block)
    {
        // <a href="/lv/filiale/..." rel="bookmark">
        //   <h3>NAME</h3>
        // </a>
        // OR (for the first few, where the link is the outer container):
        // <a href="/lv/filiale/..." rel="bookmark">
        //   <h3>NAME</h3>

        Match nameMatch = Regex.Match(
            block,
            @"<a href=""/lv/filiale/[^""]+"" rel=""bookmark"">\s*<h3>([^<]+)</h3>",
            RegexOptions.Singleline
        );

        if (!nameMatch.Success)
            throw new Exception("Did not match name in state police branch block");

        string raw = nameMatch.Groups[1].Value.Trim();

        return WebUtility.HtmlDecode(raw);
    }

    private static string? ParsePhone(string block)
    {
        // The real phone (not 112) is inside branch_contacts__branch__new-phone:
        // <div class="branch_contacts__branch__new-phone">
        //   <div>
        //     <div class="field__item field-phone">
        //       <a href="tel:+371 NNNNN" aria-label="Tālruņa numurs: +371 NNNNN">+371 NNNNN</a>

        Match phoneMatch = Regex.Match(
            block,
            @"class=""branch_contacts__branch__new-phone"".*?<a href=""tel:([^""]+)""",
            RegexOptions.Singleline
        );

        if (!phoneMatch.Success)
            return null;

        string phone = phoneMatch.Groups[1].Value.Trim();

        return phone;
    }

    private static string? ParseEmail(string block)
    {
        // Not spam-protected:
        
        // <span class="visually-hidden">E-pasts: </span><a href="mailto:pasts@vp.gov.lv" class="spamspan" aria-label="E-pasts: pasts@vp.gov.lv">pasts@vp.gov.lv</a>
        Match emailMatch = Regex.Match(
            block,
            @"href=""mailto:([^""]+)""",
            RegexOptions.Singleline
        );

        if (emailMatch.Success)
        {
            string fullEmail = emailMatch.Groups[1].Value.Trim();
            return fullEmail;
        }

        // Spam-protected variant:
        
        // <span class="spamspan"><span class="u">USER</span> [at] <span class="d">DOMAIN</span></span>
        if (!emailMatch.Success)
            emailMatch = Regex.Match(
            block,
            @"<span class=""u"">([^<]+)</span>\s*\[at\]\s*<span class=""d"">([^<]+)</span>",
            RegexOptions.Singleline
        );

        if (emailMatch.Success)
        {
            string user = emailMatch.Groups[1].Value.Trim();
            string domain = emailMatch.Groups[2].Value.Trim();

            return user + "@" + domain;
        }

        return null;
    }
}

