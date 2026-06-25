using NetLearnBattle.CSharp.Models;

namespace NetLearnBattle.CSharp.Services;

public class GameService
{
    private readonly JsonService _json;
    private readonly ScoreService _scores;
    private readonly GameSessionStore _store;
    private readonly IpService _ip;
    private readonly AclService _acl;

    public GameService(JsonService json, ScoreService scores, GameSessionStore store,
        IpService ip, AclService acl)
    {
        _json = json;
        _scores = scores;
        _store = store;
        _ip = ip;
        _acl = acl;
    }

    public GameSession StartSession(string username, int level)
    {
        var session = _store.CreateSession(username, level);
        var questions = GenerateQuestions(level);
        session.TotalQuestions = questions.Count;

        foreach (var q in questions)
            session.Questions.Enqueue(q);

        session.CurrentQuestion = session.Questions.Dequeue();
        session.QuestionStartedAt = DateTime.UtcNow;
        _store.UpdateSession(session);
        return session;
    }

    public Attempt GradeAnswer(GameSession session, int selectedIndex)
    {
        var question = session.CurrentQuestion;
        if (question == null) throw new InvalidOperationException("No current question.");
        if (selectedIndex < 0 || selectedIndex >= question.Options.Count)
            throw new ArgumentOutOfRangeException(nameof(selectedIndex), "selectedIndex inválido.");

        var isCorrect = selectedIndex == question.CorrectIndex;
        var points = isCorrect ? question.PointsCorrect : question.PointsWrong;
        var responseTimeSeconds = GetResponseTime(session.QuestionStartedAt);

        _scores.AddPoints(session.Username, points);
        var scoreAfter = _scores.GetScore(session.Username);

        var attempt = new Attempt
        {
            Username = session.Username,
            Level = session.Level,
            Topic = question.Topic,
            Question = question.QuestionText,
            SelectedAnswer = selectedIndex >= 0 && selectedIndex < question.Options.Count
                ? question.Options[selectedIndex] : string.Empty,
            CorrectAnswer = question.CorrectAnswer,
            SelectedIndex = selectedIndex,
            CorrectIndex = question.CorrectIndex,
            IsCorrect = isCorrect,
            Points = points,
            ResponseTimeSeconds = responseTimeSeconds,
            SessionId = session.SessionId,
            ScoreAfterAttempt = scoreAfter,
            CreatedAt = DateTime.UtcNow
        };

        session.Attempts.Add(attempt);

        if (isCorrect) session.CorrectAnswers++;
        else session.WrongAnswers++;

        session.PointsEarned += points;

        SaveAttempt(attempt);

        if (session.Questions.Count > 0)
        {
            session.CurrentQuestion = session.Questions.Dequeue();
            session.QuestionStartedAt = DateTime.UtcNow;
        }
        else
        {
            session.CurrentQuestion = null;
            session.QuestionStartedAt = null;
        }

        _store.UpdateSession(session);
        return attempt;
    }

    private List<Question> GenerateQuestions(int level)
    {
        var questions = new List<Question>();

        for (int i = 0; i < 5; i++)
        {
            questions.Add(level switch
            {
                1 => _ip.GenerateIpv4Question(1),
                2 => _ip.GenerateIpv4Question(2),
                3 => _ip.GenerateIpv4Question(3),
                4 => _ip.GenerateIpv6Question(),
                5 => _acl.GenerateAclQuestion(),
                _ => _ip.GenerateIpv4Question(1),
            });
        }

        return questions;
    }

    private void SaveAttempt(Attempt attempt)
    {
        var attempts = _json.LoadList<Attempt>("attempts.json");
        attempts.Add(attempt);
        _json.Save("attempts.json", attempts);
    }

    private static double GetResponseTime(DateTime? startedAt)
    {
        if (!startedAt.HasValue)
            return 0;

        var seconds = (DateTime.UtcNow - startedAt.Value).TotalSeconds;
        return seconds > 0 ? Math.Round(seconds, 2) : 0;
    }
}
