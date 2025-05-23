﻿namespace Osmalyzer;

[UsedImplicitly]
public class RezeknesSatiksmeAnalysisData : GTFSAnalysisData
{
    public override string Name => "Rezeknes Satiksme";

    public override string ReportWebLink => @"https://www.marsruti.lv/rezekne/";

        
    public override bool DataDateHasDayGranularity => true; 

    protected override string DataFileIdentifier => "rezeknes-satiksme";


    protected override string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".zip");

    public override string ExtractionFolder => "GTFS-RezSat";
        
    protected override string DataURL => @"http://www.marsruti.lv/rezekne/rezekne/gtfs.zip";
    // todo: where could I get a direct link from them? 
}