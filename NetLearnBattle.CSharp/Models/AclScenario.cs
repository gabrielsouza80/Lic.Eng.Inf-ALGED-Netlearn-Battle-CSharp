using System.Text.Json.Serialization;

namespace NetLearnBattle.CSharp.Models;

public class AclScenario
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("question")]
    public string Question { get; set; } = string.Empty;

    [JsonPropertyName("packet")]
    public Packet Packet { get; set; } = new();

    [JsonPropertyName("rules")]
    public List<AclRule> Rules { get; set; } = new();

    [JsonPropertyName("expected_action")]
    public string ExpectedAction { get; set; } = string.Empty;

    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = string.Empty;
}
