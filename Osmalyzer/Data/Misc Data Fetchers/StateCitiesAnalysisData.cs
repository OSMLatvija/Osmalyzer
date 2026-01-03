namespace Osmalyzer;

[UsedImplicitly]
public class StateCitiesAnalysisData : AnalysisData
{
    public override string Name => "State city known names";

    public override string? ReportWebLink => null;
    
    public override bool NeedsPreparation => false;
    
    protected override string DataFileIdentifier => "";

    public List<string> Names { get; private set; } = null!; // only null until loaded


    protected override void Download()
    {
        Names = [ ];
        
        string dataFileName = @"data/state cities.tsv";

        if (!File.Exists(dataFileName))
            dataFileName = @"../../../../" + dataFileName; // "exit" Osmalyzer\bin\Debug\net_.0\ folder and grab it from root data\
            
        string[] lines = File.ReadAllLines(dataFileName, Encoding.UTF8);

        Names = lines
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l))
                .ToList();
    }

    protected override void DoPrepare()
    {
        // Not doing preparation
        throw new Exception();
    }
}