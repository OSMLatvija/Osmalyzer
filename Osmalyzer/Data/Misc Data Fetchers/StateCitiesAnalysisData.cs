namespace Osmalyzer;

[UsedImplicitly]
public class StateCitiesAnalysisData : AnalysisData
{
    public override string Name => "State city known names";

    public override string? ReportWebLink => null;
    
    public override bool NeedsPreparation => false;
    
    protected override string DataFileIdentifier => "";

    public List<KnownStateCity> StateCities { get; private set; } = null!; // only null until loaded


    protected override void Download()
    {
        StateCities = [ ];
        
        string dataFileName = @"data/state cities.tsv";

        if (!File.Exists(dataFileName))
            dataFileName = @"../../../../" + dataFileName; // "exit" Osmalyzer\bin\Debug\net_.0\ folder and grab it from root data\
            
        string[] lines = File.ReadAllLines(dataFileName, Encoding.UTF8);

        StateCities = lines
                .Skip(1) // header
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l))
                .Select(MakeEntry)
                .ToList();

        KnownStateCity MakeEntry(string line)
        {
            string[] parts = line.Split('\t');
            return new KnownStateCity(
                parts[0],
                parts[1] == "yes"
            );
        }
    }

    protected override void DoPrepare()
    {
        // Not doing preparation
        throw new Exception();
    }
}


public class KnownStateCity
{
    public string Name { get; }
    public bool IndependentOfMunicipality { get; }

    public KnownStateCity(string name, bool independentOfMunicipality)
    {
        Name = name;
        IndependentOfMunicipality = independentOfMunicipality;
    }
}