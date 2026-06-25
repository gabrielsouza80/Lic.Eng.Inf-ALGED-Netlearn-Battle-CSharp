using System.Text.Json.Serialization;

namespace NetLearnBattle.CSharp.Models;

public class Packet
{
    [JsonPropertyName("src_ip")]
    public string SrcIp { get; set; } = string.Empty;

    [JsonPropertyName("dst_ip")]
    public string DstIp { get; set; } = string.Empty;

    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = string.Empty;

    [JsonPropertyName("port")]
    public int Port { get; set; }
}
