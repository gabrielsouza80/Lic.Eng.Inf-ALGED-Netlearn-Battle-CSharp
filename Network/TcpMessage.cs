using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetLearnBattle.CSharp.Network;

// [M33] Mensagem JSON genérica usada no TCP.
public class TcpMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }
}
