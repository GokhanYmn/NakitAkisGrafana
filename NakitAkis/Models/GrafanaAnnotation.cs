namespace NakitAkis.Models
{
    public class GrafanaAnnotation
    {
        public long Time { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string[] Tags { get; set; } = Array.Empty<string>();
    }
}
