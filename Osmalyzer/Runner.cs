using System.Diagnostics;

namespace Osmalyzer;

public static class Runner
{
    public static void Run()
    {
#if REMOTE_EXECUTION
        // Find all analyzer types and make an instance of each
        List<Analyzer> analyzers = 
                typeof(Analyzer)
                    .Assembly.GetTypes()
                    .Where(type => type.IsSubclassOf(typeof(Analyzer)) && !type.IsAbstract)
                    .Where(type => type.GetCustomAttributes(typeof(DisabledAnalyzerAttribute), true).Length == 0)
                    .Select(Activator.CreateInstance)
                    .Cast<Analyzer>()
                    .ToList();
#else
        List<Analyzer> analyzers =
        [
            new RigasSatiksmeAnalyzer(),
            new LiepajasTransportsAnalyzer(),
            new RezeknesSatiksmeAnalyzer(),
            new JurmalasSatiksmeAnalyzer(),
            new AutotransportaDirekcijaAnalyzer(),
            // new RigasSatiksmeTicketVendingAnalyzer(),
            // new TrolleybusWireAnalyzer(),
            // new MicroReservesAnalyzer(), // -- it fails from localhost and github runner, no idea why
            // new StreetNameAnalyzer(),
            // new RigaDrinkingWaterAnalyzer(),
            // new PublicTransportAccessAnalyzer(),
            // new HighwaySpeedLimitAnalyzer(),
            // new GlikaOaksAnalyzer(),
            // new ElviShopAnalyzer(),
            // new LatsShopAnalyzer(),
            // new RimiShopAnalyzer(),
            // new MaximaShopAnalyzer(),
            // new MegoShopAnalyzer(),
            // new AibeLatviaShopAnalyzer(),
            // todo new AibeLithuaniaShopAnalyzer(),
            // new VeskoShopAnalyzer(),
            // new SparShopAnalyzer(),
            // new CitroShopAnalyzer(),
            // new TopShopAnalyzer(),
            // new CulturalMonumentsAnalyzer(),
            // new SwedbankLocationAnalyzer(),
            // new SEBLocationAnalyzer(),
            // new CitadeleLocationAnalyzer(),
            // new LuminorLocationAnalyzer(),
            // new CourthouseAnalyzer(),
            // new NonDefiningTaggingAnalyzer(),
            // new BridgeAndWaterConnectionAnalyzer(),
            // new TerminatingWaysAnalyzer(),
            // new DoubleMappedFeaturesAnalyzer(),
            // new CrossingConsistencyAnalyzer(),
            // new StreetTaggingContinuationAnalyzer(),
            // new WikidataSynchronicityAnalyzer(), -- disabled
            // new BarrierConnectionAnalyzer(),
            // new BottleDepositPointsAnalyzer(),
            // new VenipakParcelLockerAnalyzer(),
            // new OmnivaParcelLockerAnalyzer(),
            // new SmartPostiParcelLockerAnalyzer(),
            // new DPDParcelLockerAnalyzer(),
            // new UnknownParcelLockerAnalyzer(),
            // new LatviaPostLockerAnalyzer(),
            // new LatviaPostMailBoxAnalyzer(),
            // new LatviaPostOfficeAnalyzer(),
            // new PostCodeAnalyzer(),
            // new ImproperTranslationAnalyzer(),
            // new LidlShopAnalyzer(),
            // new UnisendParcelLockerAnalyzer(),
            // new SpellingAnalyzer(),
            // new DuplicatePlatformsAnalyzer(),
            // new LoneCrossingAnalyzer(),
            // new InfoboardAnalyzer(),
            // new LuluRestaurantAnalyzer(),
            // new CaffeineRestaurantAnalyzer()
            // new HesburgerRestaurantAnalyzer()
        ];
#endif

        Console.WriteLine("Running with " + analyzers.Count + " analyzers...");


        // Prepare reporter
        // This also makes sure the output folder exists in case we want to add some extra stuff/debug there "manually"
        //Reporter reporter = new TextFileReporter();
        Reporter reporter = new HtmlFileReporter();


        List<Type> requestedDataTypes = [ ];
        List<List<Type>> perAnalyzerRequestedDataTypes = [ ];

        foreach (Analyzer analyzer in analyzers)
        {
            List<Type> analyzerRequiredDataTypes = analyzer.GetRequiredDataTypes();

            perAnalyzerRequestedDataTypes.Add(analyzerRequiredDataTypes);

            foreach (Type dataType in analyzerRequiredDataTypes)
                if (!requestedDataTypes.Contains(dataType))
                    requestedDataTypes.Add(dataType);
        }


        List<AnalysisData> requestedDatas = [ ];

        foreach (Type type in requestedDataTypes)
            requestedDatas.Add((AnalysisData)Activator.CreateInstance(type)!);


        Console.WriteLine("Retrieving data...");

        if (!Directory.Exists(AnalysisData.CacheBasePath))
        {
            Directory.CreateDirectory(AnalysisData.CacheBasePath);
        }
        else
        {
            if (!File.Exists(AnalysisData.CacheRevisionFilePath))
            {
                Console.WriteLine("Cache revision out of date, wiping...");
                Directory.Delete(AnalysisData.CacheBasePath, true);
                Directory.CreateDirectory(AnalysisData.CacheBasePath);
                File.WriteAllText(AnalysisData.CacheRevisionFilePath, "hi!");
            }
        }

        WebsiteDownloadHelper.BrowsingEnabled = true;

        for (int i = 0; i < requestedDatas.Count; i++)
        {
            Console.WriteLine("Retrieving " + requestedDatas[i].Name + " data [" + (i + 1) + "/" + requestedDatas.Count + "]...");

            Stopwatch retrieveStopwatch = Stopwatch.StartNew();

            requestedDatas[i].Retrieve();

            retrieveStopwatch.Stop();

            Console.WriteLine("(" + retrieveStopwatch.ElapsedMilliseconds + " ms)");
        }

        WebsiteDownloadHelper.BrowsingEnabled = false;


        Console.WriteLine("Preparing data...");

        List<AnalysisData> preparableData = requestedDatas.Where(rd => rd.NeedsPreparation).ToList();

        for (int i = 0; i < preparableData.Count; i++)
        {
            AnalysisData data = preparableData[i];

            if (data.Status != DataStatus.Ok)
                continue;

            Console.WriteLine("Preparing " + data.Name + " data [" + (i + 1) + "/" + preparableData.Count + "]...");

            Stopwatch prepareStopwatch = Stopwatch.StartNew();

            preparableData[i].Prepare();

            Console.WriteLine("(" + prepareStopwatch.ElapsedMilliseconds + " ms)");
        }


        Console.WriteLine("Parsing...");

        for (int i = 0; i < analyzers.Count; i++)
        {
            List<AnalysisData> datas = [ ];

            foreach (Type dataType in perAnalyzerRequestedDataTypes[i])
                datas.Add(requestedDatas.First(rd => rd.GetType() == dataType));

            if (datas.Any(d => d.Status != DataStatus.Ok))
            {
                Console.WriteLine("Skipping " + analyzers[i].Name + " analyzer due to missing required data [" + (i + 1) + "/" + analyzers.Count + "].");

                reporter.AddSkippedReport(analyzers[i].Name, "missing data");
                // todo: which one and why

                continue;
            }

            Console.Write("Parsing " + analyzers[i].Name + " analyzer [" + (i + 1) + "/" + analyzers.Count + "]... ");

            Report report = new Report(analyzers[i], datas);

            Stopwatch parseStopwatch = Stopwatch.StartNew();

            analyzers[i].Run(datas, report);

            parseStopwatch.Stop();

            Console.WriteLine(" (" + parseStopwatch.ElapsedMilliseconds + " ms)");

            reporter.AddReport(report);
        }


        Stopwatch reportStopwatch = Stopwatch.StartNew();

        Console.WriteLine("Writing reports...");

        reporter.Save();

        reportStopwatch.Stop();

        Console.WriteLine("(" + reportStopwatch.ElapsedMilliseconds + " ms)");


        Console.WriteLine("Done.");
    }
}