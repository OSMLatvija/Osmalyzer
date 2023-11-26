using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class SEBPointAnalysisData : BankPointAnalysisData, IPreparableAnalysisData
{
    public override string Name => "SEB Points";

    protected override string DataFileIdentifier => "seb-points";


    private int _pageCount;
    

    protected override void Download()
    {
        _pageCount = 1;

        for (int i = 0; i < _pageCount; i++)
        {
            string content = WebsiteDownloadHelper.Read(
                "https://www.seb.lv/atm-find?page=" + i, 
                true
            );

            if (i == 0)
            {
                // When getting first page, parse it for the total page count
                _pageCount = int.Parse(Regex.Matches(content, @"<a href=""\?page=(\d+)"" class="""" title="""">>").Last().Groups[1].ToString()) + 1;
            }

            File.WriteAllText(
                content, 
                cacheBasePath + DataFileIdentifier + "-" + i + @".html"
            );
        }
    }

    public void Prepare()
    {
        Points = new List<BankPoint>();

        for (int i = 0; i < _pageCount; i++)
        {
            string pageData = File.ReadAllText(cacheBasePath + DataFileIdentifier + "-" + i + @".html");
            
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

            MatchCollection matches = Regex.Matches(pageData, @"<div class=""mb-2"">([^<]+)</div>");

            foreach (Match match in matches)
            {
                // todo:
            }
        }

        throw new NotImplementedException();
    }
}