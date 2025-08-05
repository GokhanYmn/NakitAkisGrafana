namespace NakitAkis.Models
{
    public class GrafanaTarget
    {
        public string? Target { get; set; }
        public string? RefId { get; set; }
        public Dictionary<string, string>? Params { get; set; }
    }
}
