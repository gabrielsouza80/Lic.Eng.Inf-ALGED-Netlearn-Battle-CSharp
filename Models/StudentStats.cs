namespace NetLearnBattle.CSharp.Models;

public class StudentStats
{
    public string Username { get; set; } = string.Empty;
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public int WrongAnswers { get; set; }
    public double AccuracyRate { get; set; }
    public int CurrentScore { get; set; }
    public string WeakestTopic { get; set; } = string.Empty;
    public double AverageResponseTime { get; set; }
    public double MedianResponseTime { get; set; }
    public string ModeResponseTimeDisplay { get; set; } = "Sem dados";
    public List<LevelStats> ByLevel { get; set; } = new();
    public List<TopicStats> ByTopic { get; set; } = new();
    public List<ScoreEvolution> ScoreEvolution { get; set; } = new();
}
