namespace NetLearnBattle.CSharp.Models;

// [M09] Sessão com fila FIFO de perguntas e tentativas do aluno.
public class GameSession
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
    public string Username { get; set; } = string.Empty;
    public int Level { get; set; }
    public Queue<Question> Questions { get; set; } = new();
    public Question? CurrentQuestion { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public int WrongAnswers { get; set; }
    public int PointsEarned { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? QuestionStartedAt { get; set; }
    public List<Attempt> Attempts { get; set; } = new();

    public bool IsFinished => Questions.Count == 0 && CurrentQuestion == null;
    public int QuestionsRemaining => Questions.Count;
}
