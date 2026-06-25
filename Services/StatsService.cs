using NetLearnBattle.CSharp.Models;

namespace NetLearnBattle.CSharp.Services;

public class StatsService
{
    private readonly JsonService _json;
    private readonly ScoreService _scores;

    public StatsService(JsonService json, ScoreService scores)
    {
        _json = json;
        _scores = scores;
    }

    public StudentStats GetStudentStats(string username)
    {
        // [M18] Estatísticas do aluno usam apenas tentativas do próprio utilizador.
        var allAttempts = _json.LoadList<Attempt>("attempts.json");
        var attempts = allAttempts
            .Where(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var stats = new StudentStats
        {
            Username = username,
            TotalQuestions = attempts.Count,
            CorrectAnswers = attempts.Count(a => a.IsCorrect),
            WrongAnswers = attempts.Count(a => !a.IsCorrect),
            CurrentScore = _scores.GetScore(username),
        };

        if (stats.TotalQuestions > 0)
            stats.AccuracyRate = (double)stats.CorrectAnswers / stats.TotalQuestions * 100;

        stats.ByLevel = CalculateByLevel(attempts);
        stats.ByTopic = CalculateByTopic(attempts);
        stats.WeakestTopic = FindWeakestTopic(stats.ByTopic);

        var times = attempts
            // [M18] Tempos 0 ou ausentes não entram na média/mediana/moda.
            .Where(a => a.ResponseTimeSeconds > 0)
            .Select(a => a.ResponseTimeSeconds)
            .OrderBy(t => t)
            .ToList();

        if (times.Count > 0)
        {
            stats.AverageResponseTime = times.Average();
            stats.MedianResponseTime = CalculateMedian(times);
            stats.ModeResponseTimeDisplay = CalculateModeDisplay(times);
        }

        stats.ScoreEvolution = CalculateScoreEvolution(attempts);

        return stats;
    }

    public TeacherStats GetTeacherStats()
    {
        // [M31] Área Teacher mostra dados globais, sem dados sensíveis.
        var allAttempts = _json.LoadList<Attempt>("attempts.json");
        var stats = new TeacherStats
        {
            TotalQuestions = allAttempts.Count,
            CorrectAnswers = allAttempts.Count(a => a.IsCorrect),
            WrongAnswers = allAttempts.Count(a => !a.IsCorrect),
            Ranking = _scores.GetRanking(),
        };

        if (stats.TotalQuestions > 0)
            stats.AccuracyRate = (double)stats.CorrectAnswers / stats.TotalQuestions * 100;

        stats.ByLevel = CalculateByLevel(allAttempts);
        stats.ByTopic = CalculateByTopic(allAttempts);
        stats.Quartiles = CalculateQuartiles(_scores.GetAllScores().Values.ToList());
        stats.RecentAttempts = allAttempts
            .OrderByDescending(a => a.CreatedAt)
            .Take(20)
            .ToList();

        return stats;
    }

    private static List<LevelStats> CalculateByLevel(List<Attempt> attempts)
    {
        return attempts
            .GroupBy(a => a.Level)
            .Select(g =>
            {
                var total = g.Count();
                var correct = g.Count(a => a.IsCorrect);
                return new LevelStats
                {
                    Level = g.Key,
                    Total = total,
                    Correct = correct,
                    Wrong = total - correct,
                    AccuracyRate = total > 0 ? (double)correct / total * 100 : 0,
                };
            })
            .OrderBy(l => l.Level)
            .ToList();
    }

    private static List<TopicStats> CalculateByTopic(List<Attempt> attempts)
    {
        return attempts
            .GroupBy(a => a.Topic ?? "Sem tópico")
            .Select(g =>
            {
                var total = g.Count();
                var correct = g.Count(a => a.IsCorrect);
                return new TopicStats
                {
                    Topic = g.Key,
                    Total = total,
                    Correct = correct,
                    Wrong = total - correct,
                    AccuracyRate = total > 0 ? (double)correct / total * 100 : 0,
                };
            })
            .OrderByDescending(t => t.Total)
            .ToList();
    }

    private static string FindWeakestTopic(List<TopicStats> topics)
    {
        var valid = topics.Where(t => t.Total >= 5).ToList();
        if (valid.Count == 0) return "N/A (menos de 5 tentativas por tópico)";

        var weakest = valid.OrderBy(t => t.AccuracyRate).First();
        return $"{weakest.Topic} ({weakest.AccuracyRate:F1}%)";
    }

    private static double CalculateMedian(List<double> sorted)
    {
        int n = sorted.Count;
        if (n == 0) return 0;
        if (n % 2 == 1) return sorted[n / 2];
        return (sorted[n / 2 - 1] + sorted[n / 2]) / 2.0;
    }

    private static string CalculateModeDisplay(List<double> values)
    {
        // [M18] Moda só é mostrada quando há repetição.
        var mode = values
            .GroupBy(v => Math.Round(v, 1))
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        if (mode == null || mode.Count() <= 1)
            return "Sem moda";

        return $"{mode.Key:F1}s ({mode.Count()}x)";
    }

    private static List<ScoreEvolution> CalculateScoreEvolution(List<Attempt> attempts)
    {
        // [M18] Evolução junta tentativas por sessão para explicar progresso real.
        return attempts
            .GroupBy(a => string.IsNullOrWhiteSpace(a.SessionId)
                ? $"tentativa-{a.CreatedAt.Ticks}" : a.SessionId)
            .Select(group =>
            {
                var ordered = group.OrderBy(a => a.CreatedAt).ToList();
                var lastAttempt = ordered.Last();
                return new ScoreEvolution
                {
                    SessionId = group.Key,
                    CreatedAt = ordered.First().CreatedAt,
                    Level = ordered.First().Level,
                    TotalQuestions = ordered.Count,
                    CorrectAnswers = ordered.Count(a => a.IsCorrect),
                    WrongAnswers = ordered.Count(a => !a.IsCorrect),
                    SessionPoints = ordered.Sum(a => a.Points),
                    Score = lastAttempt.ScoreAfterAttempt
                };
            })
            .OrderBy(e => e.CreatedAt)
            .ToList();
    }

    private static QuartileStats CalculateQuartiles(List<int> scores)
    {
        // [M18] Quartis resumem a distribuição dos scores.
        scores = scores.OrderBy(s => s).ToList();

        if (scores.Count == 0) return new QuartileStats();

        return new QuartileStats
        {
            Min = scores.First(),
            Q1 = Percentile(scores, 25),
            Median = Percentile(scores, 50),
            Q3 = Percentile(scores, 75),
            Max = scores.Last(),
        };
    }

    private static int Percentile(List<int> sorted, int percentile)
    {
        int n = sorted.Count;
        if (n == 0) return 0;
        if (n == 1) return sorted[0];

        double index = percentile / 100.0 * (n - 1);
        int lower = (int)Math.Floor(index);
        int upper = (int)Math.Ceiling(index);

        if (lower == upper) return sorted[lower];

        double frac = index - lower;
        return (int)Math.Round(sorted[lower] + frac * (sorted[upper] - sorted[lower]));
    }
}
