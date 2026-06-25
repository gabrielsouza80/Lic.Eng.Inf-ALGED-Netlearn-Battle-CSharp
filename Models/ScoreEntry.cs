using System.Text.Json.Serialization;

namespace NetLearnBattle.CSharp.Models;

// [M13] Entrada pública do ranking: apenas username e score.
public class ScoreEntry
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public int Score { get; set; }
}
