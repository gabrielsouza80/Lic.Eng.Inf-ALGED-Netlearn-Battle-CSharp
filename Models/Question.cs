using System.Text.Json.Serialization;

namespace NetLearnBattle.CSharp.Models;

public class Question
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public int Level { get; set; }

    public string Topic { get; set; } = string.Empty;

    public string QuestionType { get; set; } = string.Empty;

    [JsonPropertyName("question")]
    public string QuestionText { get; set; } = string.Empty;

    [JsonIgnore]
    public string Text => QuestionText;

    public List<string> Options { get; set; } = new();

    [JsonPropertyName("correct_index")]
    public int CorrectIndex { get; set; }

    [JsonIgnore]
    public string CorrectAnswer => Options.Count > CorrectIndex ? Options[CorrectIndex] : string.Empty;

    [JsonPropertyName("points_correct")]
    public int PointsCorrect { get; set; }

    [JsonPropertyName("points_wrong")]
    public int PointsWrong { get; set; }
}
