namespace NakitAkis.Models
{
    public class GrafanaAnnotationRequest
    {
        public GrafanaRange Range { get; set; } = new();
        public Dictionary<string, string>? Annotation { get; set; }
    }
}
