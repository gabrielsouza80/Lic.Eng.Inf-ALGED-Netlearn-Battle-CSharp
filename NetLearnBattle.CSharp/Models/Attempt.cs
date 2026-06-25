using System.Text.Json.Serialization;

namespace NetLearnBattle.CSharp.Models;

public class Attempt
{
    public string Username { get; set; } = string.Empty;
    public int Level { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string SelectedAnswer { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public int SelectedIndex { get; set; }
    public int CorrectIndex { get; set; }
    public bool IsCorrect { get; set; }
    public int Points { get; set; }
    [JsonConverter(typeof(FlexibleDoubleConverter))]
    public double ResponseTimeSeconds { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public int ScoreAfterAttempt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
