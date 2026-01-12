namespace Osmalyzer;

[UsedImplicitly]
public class CspPopulationAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public override string Name => "CSP Population Statistics";

    public override string ReportWebLink => @"https://data.stat.gov.lv/pxweb/lv/OSP_PUB/START__POP__IR__IRS/IRS051/";

    public override bool NeedsPreparation => true;


    protected override string DataFileIdentifier => "csp";


    public List<CspPopulationEntry> Entries { get; private set; } = null!; // only null before prepared


    protected override void Download()
    {
        // todo
    }

    protected override void DoPrepare()
    {
        Entries = [ ];
        
        // todo
    }
}


public class CspPopulationEntry : IDataItem
{
    public long ID { get; }

    public OsmCoord Coord => throw new NotImplementedException();
    
    public string Name => throw new NotImplementedException();
    
   
    public CspPopulationEntry(long id)
    {
        ID = id;
    }
    
    
    public string ReportString()
    {
        return
            "#" + ID;
    }
}