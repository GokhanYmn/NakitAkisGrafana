namespace NakitAkis.Models
{
    public class GrafanaQueryResult
    {
        public string Target { get; set; } = string.Empty;
        public List<decimal[]> DataPoints { get; set; } = new();
    }
}
