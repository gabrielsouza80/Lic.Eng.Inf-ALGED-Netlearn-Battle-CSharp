namespace NetLearnBattle.CSharp.Models;

// [M18] Estatísticas globais usadas na área do professor.
public class TeacherStats
{
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public int WrongAnswers { get; set; }
    public double AccuracyRate { get; set; }
    public List<LevelStats> ByLevel { get; set; } = new();
    public List<TopicStats> ByTopic { get; set; } = new();
    public List<ScoreEntry> Ranking { get; set; } = new();
    public QuartileStats Quartiles { get; set; } = new();
    public List<Attempt> RecentAttempts { get; set; } = new();
}
