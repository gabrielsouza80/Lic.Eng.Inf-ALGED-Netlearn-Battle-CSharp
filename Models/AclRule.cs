using System.Text.Json.Serialization;

namespace NetLearnBattle.CSharp.Models;

// [M17] Regra ACL usada para permitir ou negar pacotes.
public class AclRule
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = string.Empty;

    [JsonPropertyName("src")]
    public string Src { get; set; } = string.Empty;

    [JsonPropertyName("dst")]
    public string Dst { get; set; } = string.Empty;

    [JsonPropertyName("port")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Port { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
