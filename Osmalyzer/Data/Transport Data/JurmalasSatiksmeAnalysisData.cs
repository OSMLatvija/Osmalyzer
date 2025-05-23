﻿namespace Osmalyzer;

[UsedImplicitly]
public class JurmalasSatiksmeAnalysisData : GTFSAnalysisData
{
    public override string Name => "Jurmalas Autobusu Satiksme";

    public override string ReportWebLink => @"https://www.marsruti.lv/jurmala/";


    public override bool DataDateHasDayGranularity => true;

    protected override string DataFileIdentifier => "jurmalas-satiksme";


    protected override string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".zip");

    public override string ExtractionFolder => "GTFS-JurSat";
        
    protected override string DataURL => @"http://www.marsruti.lv/jurmala/jurmala/gtfs.zip";
    // todo: where could I get a direct link from them? 
}