namespace Osmalyzer;

[UsedImplicitly]
public class ParcelLockerOperatorAnalysisData : AnalysisData
{
    public override string Name => "Parcel Locker operators";

    public override string? ReportWebLink => null;
    
    public override bool NeedsPreparation => false;

    
    protected override string DataFileIdentifier => "parcel-locker-operators";


    public Dictionary<string, List<string>> Branding { get; private set; } = null!; // only null until downloaded


    protected override void Download()
    {
        Branding = new Dictionary<string, List<string>>();
        
        string dataFileName = @"data/parcel locker operators.tsv";

        if (!File.Exists(dataFileName))
            dataFileName = @"../../../../" + dataFileName; // "exit" Osmalyzer\bin\Debug\net6.0\ folder and grab it from root data\
            
        string[] lines = File.ReadAllLines(dataFileName, Encoding.UTF8);

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            
            if (line.StartsWith("// "))
                continue;
            
            string[] splits = line.Split('\t');
                
            if (splits.Length == 1) throw new Exception();
                
            Branding.Add(splits[0], splits.Skip(1).ToList());
        }
    }

    protected override void DoPrepare()
    {
        // Not doing preparation
        throw new Exception();
    }
}