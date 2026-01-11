using System.Diagnostics;

namespace Osmalyzer;

public static class Runner
{
    public static void Run()
    {
        Console.OutputEncoding = Encoding.UTF8;
        
#if REMOTE_EXECUTION
        // Find all analyzer types and make an instance of each (including disabled; we'll report and skip them below)
        List<Analyzer> analyzers = 
                typeof(Analyzer)
                    .Assembly.GetTypes()
                    .Where(type => type.IsSubclassOf(typeof(Analyzer)) && !type.IsAbstract)
                    .Select(Activator.CreateInstance)
                    .Cast<Analyzer>()
                    .ToList();
#else
        List<Analyzer> analyzers =
        [
            // new RigasSatiksmeAnalyzer(),
            // new LiepajasTransportsAnalyzer(),
            // new RezeknesSatiksmeAnalyzer(),
            // new JurmalasSatiksmeAnalyzer(),
            // new AutotransportaDirekcijaAnalyzer(),
            // new JelgavasAutobusuParksAnalyzer(),
            // new RigasSatiksmeTicketVendingAnalyzer(),
            // new TrolleybusWireAnalyzer(),
            // new MicroReservesAnalyzer(), // -- it fails from localhost and github runner, no idea why
            // new StreetNameAnalyzer(),
            // new RigaDrinkingWaterAnalyzer(),
            // new PublicTransportAccessAnalyzer(),
            // new HighwaySpeedLimitAnalyzer(),
            // new GlikaOaksAnalyzer(),
            // new CityMeadowsAnalyzer(),
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
            // new StreetTaggingContinuityAnalyzer(),
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
            // new BarrierAnalyzer(),
            // new InfoboardAnalyzer(),
            // new LuluRestaurantAnalyzer(),
            // new CaffeineRestaurantAnalyzer(),
            // new HesburgerRestaurantAnalyzer(),
            // new LVCRoadAnalyzer(),
            // new VPVKACAnalyzer(),
            new VillageAnalyzer(),
            new HamletAnalyzer(),
            new MunicipalityAnalyzer(),
            new ParishAnalyzer(),
            new CityAnalyzer(),
            // new VdbAnalyzer(),
            // new MaxspeedTypeAnalyzer(),
            // new LVMPicnicSiteAnalyzer(),
            // new RestrictionRelationAnalyzer(),
            // new LifecycleLeftoversAnalyzer()
        ];
#endif

        // Prepare reporter
        // This also makes sure the output folder exists in case we want to add some extra stuff/debug there "manually"
        //Reporter reporter = new TextFileReporter();
        Reporter reporter = new HtmlFileReporter();

        // Filter out analyzers that are disabled or require disabled data, and build the data requirements for the rest
        List<Analyzer> analyzersToRun = [ ];
        List<Type> requestedDataTypes = [ ];
        List<List<Type>> perAnalyzerRequestedDataTypes = [ ];

        foreach (Analyzer analyzer in analyzers)
        {
            // Skip analyzers explicitly disabled by attribute
            DisabledAnalyzerAttribute? disabledAnalyzerAttr = (DisabledAnalyzerAttribute?)analyzer.GetType().GetCustomAttributes(typeof(DisabledAnalyzerAttribute), true).FirstOrDefault();
            if (disabledAnalyzerAttr != null)
            {
                reporter.AddSkippedReport(analyzer.Name, "disabled");
                continue;
            }

            List<Type> analyzerRequiredDataTypes = analyzer.GetRequiredDataTypes();

            // Skip analyzers if any required data type is disabled
            List<(Type type, DisabledDataAttribute attr)> disabledData = analyzerRequiredDataTypes
                .Select(t => (type: t, attr: (DisabledDataAttribute?)t.GetCustomAttributes(typeof(DisabledDataAttribute), true).FirstOrDefault()))
                .Where(p => p.attr != null)
                .Select(p => (p.type, p.attr!))
                .ToList();

            if (disabledData.Count > 0)
            {
                reporter.AddSkippedReport(analyzer.Name, "required data disabled");
                continue;
            }

            analyzersToRun.Add(analyzer);
            perAnalyzerRequestedDataTypes.Add(analyzerRequiredDataTypes);

            foreach (Type dataType in analyzerRequiredDataTypes)
                if (!requestedDataTypes.Contains(dataType))
                    requestedDataTypes.Add(dataType);
        }

        Console.WriteLine("Running with " + analyzersToRun.Count + " analyzers...");


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

        for (int i = 0; i < analyzersToRun.Count; i++)
        {
            List<AnalysisData> datas = [ ];

            foreach (Type dataType in perAnalyzerRequestedDataTypes[i])
                datas.Add(requestedDatas.First(rd => rd.GetType() == dataType));

            if (datas.Any(d => d.Status != DataStatus.Ok))
            {
                Console.WriteLine("Skipping " + analyzersToRun[i].Name + " analyzer due to missing required data [" + (i + 1) + "/" + analyzersToRun.Count + "].");

                reporter.AddSkippedReport(analyzersToRun[i].Name, "missing/broken data");
                // todo: which one and why

                continue;
            }

            Console.Write("Parsing " + analyzersToRun[i].Name + " analyzer [" + (i + 1) + "/" + analyzersToRun.Count + "]... ");

            Report report = new Report(analyzersToRun[i], datas);

            Stopwatch parseStopwatch = Stopwatch.StartNew();

            analyzersToRun[i].Run(datas, report);

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