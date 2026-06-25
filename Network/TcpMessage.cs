using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetLearnBattle.CSharp.Network;

public class TcpMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }
}
