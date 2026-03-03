namespace Osmalyzer;

public class OsmElementPreviewValue : CorrelatorParamater
{
    public string Tag { get; }
    
    public bool ShowTag { get; }

    public PreviewLabel[] Labels { get; }

    
    public OsmElementPreviewValue(string tag, bool showTag, params PreviewLabel[] labels)
    {
        Tag = tag;
        ShowTag = showTag;
        Labels = labels;
    }
    

    public class PreviewLabel
    {
        public string Value { get; }
        
        public string Label { get; }

        
        public PreviewLabel(string value, string label)
        {
            Value = value;
            Label = label;
        }
    }
}