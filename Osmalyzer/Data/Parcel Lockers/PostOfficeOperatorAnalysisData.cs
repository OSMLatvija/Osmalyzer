namespace Osmalyzer;

[UsedImplicitly]
public class PostOfficeOperatorAnalysisData : AnalysisData
{
    public override string Name => "Post Office operators";

    public override string? ReportWebLink => null;
    
    public override bool NeedsPreparation => false;

    
    protected override string DataFileIdentifier => "post-office-operators";


    public List<string> Operators { get; private set; } = null!; // only null until downloaded


    protected override void Download()
    {
        Operators = new List<string>();
        
        string dataFileName = @"data/post office operators.tsv";

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
                
            Operators.Add(splits[0]);
        }
    }

    protected override void DoPrepare()
    {
        // Not doing preparation
        throw new Exception();
    }
}