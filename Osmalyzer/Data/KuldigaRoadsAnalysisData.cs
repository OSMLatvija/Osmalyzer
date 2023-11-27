using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class KuldigaRoadsAnalysisData : AnalysisData, ICachableAnalysisData, IPreparableAnalysisData
{
    public override string Name => "Kuldiga Roads";

    public bool DataDateHasDayGranularity => false; // only day given on data page

    protected override string DataFileIdentifier => "kuldiga-roads";


    public List<string> RoadNames { get; private set; } = null!; // only null until prepared


    public DateTime RetrieveDataDate()
    {
        string result = WebsiteDownloadHelper.Read("https://www.kuldiga.lv/pasvaldiba/publiskie-dokumenti/autocelu-klases", true);

        Match dateMatch = Regex.Match(result, @"Publicēts (\d{1,2})\.(\d{1,2})\.(\d{1,4})\s*(\d{1,2}):(\d{1,2})");
        int newestYear = int.Parse(dateMatch.Groups[3].ToString());
        int newestMonth = int.Parse(dateMatch.Groups[2].ToString());
        int newestDay = int.Parse(dateMatch.Groups[1].ToString());
        int newestHour = int.Parse(dateMatch.Groups[4].ToString());
        int newestMinute = int.Parse(dateMatch.Groups[5].ToString());
            
        return new DateTime(newestYear, newestMonth, newestDay, newestHour, newestMinute, 0);
    }

    protected override void Download()
    {
        if (!Directory.Exists(cacheBasePath + DataFileIdentifier))
            Directory.CreateDirectory(cacheBasePath + DataFileIdentifier);

        string result = WebsiteDownloadHelper.Read("https://www.kuldiga.lv/pasvaldiba/publiskie-dokumenti/autocelu-klases", true);

        MatchCollection urlMatches = Regex.Matches(result, @"<a href=""(/images/Faili/Pasvaldiba/autoceli/[^""]+)"">");

        for (int i = 0; i < urlMatches.Count; i++)
        {
            string url = @"https://www.kuldiga.lv" + urlMatches[i].Groups[1];

            WebsiteDownloadHelper.Download(
                url,
                cacheBasePath + DataFileIdentifier + "/" + (i + 1) + ".pdf"
            );
        }
    }

    public void Prepare()
    {
        RoadNames = new List<string>();
            
        string[] files = Directory.GetFiles(cacheBasePath + DataFileIdentifier + "/", "*.pdf");

        for (int i = 0; i < files.Length; i++)
        {
            using PdfReader reader = new PdfReader(files[i]);

            using PdfDocument document = new PdfDocument(reader);

            for (int pageNumber = 1; pageNumber <= document.GetNumberOfPages(); pageNumber++)
            {
                SimpleTextExtractionStrategy strategy = new SimpleTextExtractionStrategy();

                string pageText = PdfTextExtractor.GetTextFromPage(document.GetPage(pageNumber), strategy);

                //File.WriteAllText(i + pageNumber + ".txt", pageText);

                // 50 Dārzniecības iela 0,897 C
                    
                // 13 6242B007 Kļaviņas – Gaņģīši 1,87 D 15 Stacijas iela 1,12 C
                    
                // 48 6242C024 Izgāztuves ceļš 0,78 D
                    
                // 15
                // 6254C001 Žubītes – Jocēni
                // 0,35 D                   
                    
                // 1
                // 6254A001 Krastnieki-Mazīvande 6,81 C                    

                MatchCollection matches = Regex.Matches(pageText, @"(?<![,\d])\d{1,2}\s*(?:\d{4}[ABC]\d{3}\s*)?([^\n,]{8,}?)\s*\d+,\d+");
                // at some point regexes just look like I randomly mashed the keyboard

                if (matches.Count == 0) throw new Exception("Something wrong with PDF or extracted text");

                foreach (Match match in matches)
                {
                    RoadNames.Add(match.Groups[1].ToString());
                }
            }
        }
    }
}