using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Osmalyzer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
#if REMOTE_EXECUTION
            string executionPoint = "Remote";
#else
            string executionPoint = "Local";
#endif
            Console.WriteLine(executionPoint + " execution (running from \"" + Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\", current path at \"" + Directory.GetCurrentDirectory() + "\")");
            
            
#if REMOTE_EXECUTION
            // Find all analyzer types and make an instance of each
            List<Analyzer> analyzers = 
                    typeof(Analyzer)
                        .Assembly.GetTypes()
                        .Where(type => type.IsSubclassOf(typeof(Analyzer)) && !type.IsAbstract)
                        .Select(Activator.CreateInstance)
                        .Cast<Analyzer>()
                        .ToList();
#else
            List<Analyzer> analyzers = new List<Analyzer>()
            {
                new RigasSatiksmeAnalyzer(),
                //new LVCRoadAnalyzer(),
                //new HighwaySpeedConditionalAnalyzer(),
                //new TrolleybusWireAnalyzer(),
                //new CommonBrandsAnalyzer(),
            };
#endif

            Console.WriteLine("Running with " + analyzers.Count + " analyzers...");

            
            List<Type> requestedDataTypes = new List<Type>();
            List<List<Type>> perAnalayzerRequestedDataTypes = new List<List<Type>>();
            
            foreach (Analyzer analyzer in analyzers)
            {
                List<Type> analyzerRequiredDataTypes = analyzer.GetRequiredDataTypes();

                perAnalayzerRequestedDataTypes.Add(analyzerRequiredDataTypes);
                
                foreach (Type dataType in analyzerRequiredDataTypes)
                    if (!requestedDataTypes.Contains(dataType))
                        requestedDataTypes.Add(dataType);
            }

            
            List<AnalysisData> requestedDatas = new List<AnalysisData>();

            foreach (Type type in requestedDataTypes)
                requestedDatas.Add((AnalysisData)Activator.CreateInstance(type)!);

            
            Console.WriteLine("Retrieving data...");

            if (!Directory.Exists("cache/"))
                Directory.CreateDirectory("cache/");

            for (int i = 0; i < requestedDatas.Count; i++)
            {
                Console.WriteLine("Retrieving " + requestedDatas[i].Name + " data [" + (i + 1) + "/" + requestedDatas.Count + "]...");

                requestedDatas[i].Retrieve();
            }


            Console.WriteLine("Preparing data...");

            foreach (AnalysisData data in requestedDatas)
                data.Prepare();


            Console.WriteLine("Parsing...");

            //Reporter reporter = new TextFileReporter();
            Reporter reporter = new HtmlFileReporter();
            
            for (int i = 0; i < analyzers.Count; i++)
            {
                Console.WriteLine("Parsing " + analyzers[i].Name + " analyzer [" + (i + 1) + "/" + analyzers.Count + "]...");

                List<AnalysisData> datas = new List<AnalysisData>();

                foreach (Type dataType in perAnalayzerRequestedDataTypes[i])
                    datas.Add(requestedDatas.First(rd => rd.GetType() == dataType));

                Report report = new Report(analyzers[i], datas);
                
                analyzers[i].Run(datas, report);

                reporter.AddReport(report);
            }

            
            Console.WriteLine("Writing reports...");
            
            reporter.Save();

#if !REMOTE_EXECUTION
            // todo: if only one analyzer enabled, then auto-launch the report
#endif

            
            Console.WriteLine("Done.");
        }
    }
}