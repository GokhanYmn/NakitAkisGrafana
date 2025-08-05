namespace NakitAkis.Models
{
    public class GrafanaVariableRequest
    {
        public string? Variable { get; set; }
        public Dictionary<string, string>? Params { get; set; }
    }
}
