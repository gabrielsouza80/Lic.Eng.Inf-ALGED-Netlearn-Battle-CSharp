namespace NetLearnBattle.CSharp.Models;

public class ScoreEvolution
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int Level { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public int WrongAnswers { get; set; }
    public int SessionPoints { get; set; }
    public int Score { get; set; }
}
