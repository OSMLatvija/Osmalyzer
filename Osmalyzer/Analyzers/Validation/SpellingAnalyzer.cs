namespace Osmalyzer;

[UsedImplicitly]
public class SpellingAnalyzer : Analyzer
{
    public override string Name => "Spelling";

    public override string Description => "This analyzer checks spelling. WIP.";

    public override AnalyzerGroup Group => AnalyzerGroup.Validation;


    public override List<Type> GetRequiredDataTypes() =>
    [
        typeof(LatviaOsmAnalysisData),
        typeof(LatvianHunspellDictionaryAnalysisData),
        typeof(LatvianCustomDictionaryAnalysisData)
    ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
        
        OsmData OsmData = osmData.MasterData;

        OsmData osmElements = OsmData.Filter(
            new IsWay(),
            new HasKey("name"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.FuzzyLoose)
        );
        
        // Spellcheck
        
        LatvianHunspellDictionaryAnalysisData hunspellData = datas.OfType<LatvianHunspellDictionaryAnalysisData>().First();
        LatvianCustomDictionaryAnalysisData customData = datas.OfType<LatvianCustomDictionaryAnalysisData>().First();

        // Parse

        Spellchecker spellchecker = new Spellchecker(hunspellData, customData);

        // Collect problems grouped by problematic value, so we can list then in bulk
        List<Problem> problems = new List<Problem>();
        
        // Avoid checking things we've seen and decided are ok
        HashSet<string> okValues = new HashSet<string>();

        int ok = 0;
        int misspelled = 0;
        
        foreach (OsmElement element in osmElements.Elements)
        {
            string rawName = element.GetValue("name")!;

            const char temp = '\uFFFD';

            if (element.HasValue("public_transport", "platform")) // some stops can have double names, this is fine
            {
                rawName = rawName.Replace('/', temp);
            }
            else
            {
                // Split by ; as multi-vlaues or / as different language alternatives

                // Make sure to preserve known "/" uses
                // (note that we can't just split with " / ", because many names don't add such space)
                string[] knownUses =
                {
                    @"(A)/(S)", // akciju sabiedrība
                    @"(T)/(C)", // tirdzniecības centrs
                    @"(T)/(P)", // tirdzniecības parks
                    @"(B)/(C)", // biznesa centrs
                    @"(a)/(c)", // autoceļš
                    @"(Z)/(S)", // zemnieku sabiedrība
                    @"(K)/(S)", // kooperatīvā? sabiedrība
                    @"(D)/(B)", // dārzkopības biedrība
                    @"(I)/(U)", // individuālais uzņēmums
                    @"(\d+\.?)/(\d+)" // 2023/2024 or 24/7 or 110/110kV
                };
                foreach (string knownUse in knownUses)
                    rawName = Regex.Replace(rawName, knownUse, $@"$1{temp}$2", RegexOptions.IgnoreCase);
            }

            List<string> parts = rawName.Split(';', '/')
                                        .Select(p => p.Trim().Replace(temp, '/'))
                                        .Where(p => p != "")
                                        .ToList();

            rawName = rawName.Replace(temp, '/');

            List<(string, string)>? prefixedValues = null;
            
            if (parts.Count > 1)
                prefixedValues = element.GetPrefixedValues("name:");

            foreach (string part in parts)
            {
                if (parts.Count > 1)
                {
                    if (prefixedValues != null)
                    {
                        // Check if one of the prefixed names have the part and we know the language is something we can't parse
                        // For example, `name=Laikupe / Lätioja` and `name:lv=Laikupe` + `name:et=Lätioja`
                        // We know we can ignore `name:et` that matches "Lätioja" and only check the other part.
                        // (Note that we can only do this for multi-part names, because main `name=Xxx` doesn't mean some random `name:xx` cannot also be "Xxx")

                        bool foundPartAsFullNameOfAnotherLanguage = false;

                        foreach ((string key, string value) in prefixedValues)
                        {
                            string? language = ImproperTranslationAnalyzer.ExtractLanguage(key);

                            if (language == null)
                                continue; // key is not for a language

                            if (language == "lv") // todo: others
                                continue; // this is the language we actually spell-checking, so even if we match, we can't just skip - who says the name:lv is spelled right?

                            if (value == part) // e.g. "Lätioja" == `name:et=Lätioja`
                            {
                                foundPartAsFullNameOfAnotherLanguage = true;
                                break;
                            }
                        }

                        if (foundPartAsFullNameOfAnotherLanguage)
                            continue; // this part is fine then as is
                    }
                }
                

                Problem? existingProblem = problems.FirstOrDefault(p => p.Value == rawName && p.Part == part);
                
                if (existingProblem != null)
                {
                    // Already decided this value/part is a problem

                    misspelled++;
                    existingProblem.Elements.Add(element);
                }
                else if (!okValues.Contains(part))
                {
                    // Not yet seen this value/part, will now check if it's good/bad

                    SpellcheckResult result = spellchecker.Check(part);

                    switch (result)
                    {
                        case OkaySpellcheckResult:
                            ok++;
                            // Don't recheck this in futrure
                            okValues.Add(part);
                            break;

                        case MisspelledSpellcheckResult misspelledResult:
                            misspelled++;
                            problems.Add(new Problem(rawName, part, element, misspelledResult));
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(result));
                    }
                }
                else
                {
                    // Seen this value before and it was ok

                    ok++;
                }
            }
        }
        
        // Report
        
        report.AddGroup(
            ReportGroup.SpellingIssues, 
            "Spelling issues",
            "Note that this uses a spellchecking dictionary, so it doesn't know about proper names and things like brands. This is currently an exercise in false positives."
        );

        report.AddEntry(
            ReportGroup.SpellingIssues,
            new GenericReportEntry(
                "There are " + problems.Count + " unknown-spelling values from " + misspelled + " (out of " + ok + ") elements"
            )
        );

        foreach (Problem problem in problems)
        {
            report.AddEntry(
                ReportGroup.SpellingIssues,
                new IssueReportEntry(
                    (problem.Elements.Count > 1 ? problem.Elements.Count + " elements have " : "Element has ") +
                    " name `" + problem.Value + "` " +
                    (problem.Part != problem.Value ? " part `" + problem.Part + "`" : "") +
                    " with " +
                    (problem.Result.Misspellings.Count > 1 ? problem.Result.Misspellings.Count + " unknown spellings" : "an unknown spelling") + ": " +
                    string.Join(", ", problem.Result.Misspellings.Select(m => "`" + m.Word + "`")) +
                    " -- " + string.Join(", ", problem.Elements.Take(10).Select(e => e.OsmViewUrl)) +
                    (problem.Elements.Count > 10 ? " and " + (problem.Elements.Count - 10) + " more" : ""),
                    new SortEntryDesc(problem.Elements.Count)
                )
            );
        }
    }
    
    
    private class Problem
    {
        public string Value { get; }
        
        public string Part { get; }
        
        public List<OsmElement> Elements { get; }

        public MisspelledSpellcheckResult Result { get; }

        
        public Problem(string value, string part, OsmElement element, MisspelledSpellcheckResult result)
        {
            Value = value;
            Part = part;
            Elements = new List<OsmElement>() { element };
            Result = result;
        }
    }
    
    
    private enum ReportGroup
    {
        SpellingIssues
    }
}