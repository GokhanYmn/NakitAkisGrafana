namespace NakitAkis.Models
{
    public class GrafanaQueryRequest
    {
        public GrafanaTarget[] Targets { get; set; } = Array.Empty<GrafanaTarget>();
        public GrafanaRange Range { get; set; } = new();
        public int MaxDataPoints { get; set; } = 1000;
    }
}
