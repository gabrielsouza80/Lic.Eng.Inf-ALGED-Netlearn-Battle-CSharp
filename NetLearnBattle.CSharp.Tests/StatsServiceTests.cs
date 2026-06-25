using NetLearnBattle.CSharp.Models;

namespace NetLearnBattle.CSharp.Tests;

public class StatsServiceTests
{
    private static (string dir, JsonService json, ScoreService scores, StatsService stats) CreateWithSeed()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();

        var scores = new ScoreService(json);
        scores.AddPoints("aluno1", 150);
        scores.AddPoints("aluno2", 300);

        var attempts = new List<Attempt>
        {
            new() { Username = "aluno1", Level = 1, Topic = "IPv4 básico", IsCorrect = true, Points = 10, ResponseTimeSeconds = 2.5, SessionId = "s1", ScoreAfterAttempt = 10, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
            new() { Username = "aluno1", Level = 1, Topic = "IPv4 básico", IsCorrect = true, Points = 10, ResponseTimeSeconds = 3.0, SessionId = "s1", ScoreAfterAttempt = 20, CreatedAt = DateTime.UtcNow.AddMinutes(-9) },
            new() { Username = "aluno1", Level = 1, Topic = "IPv4 básico", IsCorrect = false, Points = -5, ResponseTimeSeconds = 1.5, SessionId = "s1", ScoreAfterAttempt = 15, CreatedAt = DateTime.UtcNow.AddMinutes(-8) },
            new() { Username = "aluno1", Level = 2, Topic = "Sub-redes IPv4", IsCorrect = true, Points = 20, ResponseTimeSeconds = 4.0, SessionId = "s2", ScoreAfterAttempt = 35, CreatedAt = DateTime.UtcNow.AddMinutes(-7) },
            new() { Username = "aluno1", Level = 2, Topic = "Sub-redes IPv4", IsCorrect = false, Points = -10, ResponseTimeSeconds = 6.0, SessionId = "s2", ScoreAfterAttempt = 25, CreatedAt = DateTime.UtcNow.AddMinutes(-6) },
            new() { Username = "aluno2", Level = 1, Topic = "IPv4 básico", IsCorrect = true, Points = 10, ResponseTimeSeconds = 0.8, SessionId = "s3", ScoreAfterAttempt = 10, CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
        };
        json.Save("attempts.json", attempts);
        scores.AddPoints("aluno1", 150);

        var stats = new StatsService(json, scores);
        return (dir, json, scores, stats);
    }

    [Fact]
    public void GetStudentStats_ReturnsCorrectTotals()
    {
        var (dir, _, _, stats) = CreateWithSeed();
        try
        {
            var s = stats.GetStudentStats("aluno1");
            Assert.Equal(5, s.TotalQuestions);
            Assert.Equal(3, s.CorrectAnswers);
            Assert.Equal(2, s.WrongAnswers);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void GetStudentStats_AccuracyRate()
    {
        var (dir, _, _, stats) = CreateWithSeed();
        try
        {
            var s = stats.GetStudentStats("aluno1");
            Assert.Equal(60.0, s.AccuracyRate, 1);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void GetStudentStats_ByLevel()
    {
        var (dir, _, _, stats) = CreateWithSeed();
        try
        {
            var s = stats.GetStudentStats("aluno1");
            Assert.Equal(2, s.ByLevel.Count);
            var lvl1 = s.ByLevel.First(l => l.Level == 1);
            Assert.Equal(3, lvl1.Total);
            Assert.Equal(2, lvl1.Correct);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void GetStudentStats_ByTopic()
    {
        var (dir, _, _, stats) = CreateWithSeed();
        try
        {
            var s = stats.GetStudentStats("aluno1");
            Assert.Contains(s.ByTopic, t => t.Topic == "IPv4 básico");
            Assert.Contains(s.ByTopic, t => t.Topic == "Sub-redes IPv4");
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void GetStudentStats_ResponseTimes()
    {
        var (dir, _, _, stats) = CreateWithSeed();
        try
        {
            var s = stats.GetStudentStats("aluno1");
            Assert.True(s.AverageResponseTime > 0);
            Assert.True(s.MedianResponseTime > 0);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void GetStudentStats_ScoreEvolution()
    {
        var (dir, _, _, stats) = CreateWithSeed();
        try
        {
            var s = stats.GetStudentStats("aluno1");
            Assert.NotEmpty(s.ScoreEvolution);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void GetStudentStats_WeakestTopic_Requires5Attempts()
    {
        var (dir, _, _, stats) = CreateWithSeed();
        try
        {
            var s = stats.GetStudentStats("aluno1");
            // Neither topic has >= 5 attempts for aluno1
            Assert.Contains("N/A", s.WeakestTopic);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void GetTeacherStats_ReturnsGlobal()
    {
        var (dir, _, _, stats) = CreateWithSeed();
        try
        {
            var t = stats.GetTeacherStats();
            Assert.Equal(6, t.TotalQuestions);
            Assert.Equal(4, t.CorrectAnswers);
            Assert.Equal(2, t.WrongAnswers);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void GetTeacherStats_IncludesRanking()
    {
        var (dir, _, _, stats) = CreateWithSeed();
        try
        {
            var t = stats.GetTeacherStats();
            Assert.NotEmpty(t.Ranking);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void GetTeacherStats_IncludesRecentAttempts()
    {
        var (dir, _, _, stats) = CreateWithSeed();
        try
        {
            var t = stats.GetTeacherStats();
            Assert.NotEmpty(t.RecentAttempts);
            Assert.True(t.RecentAttempts.Count <= 20);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void GetTeacherStats_Quartiles()
    {
        var (dir, _, _, stats) = CreateWithSeed();
        try
        {
            var t = stats.GetTeacherStats();
            // aluno1=300, aluno2=300 (both have 300 after setup)
            Assert.Equal(300, t.Quartiles.Min);
            Assert.Equal(300, t.Quartiles.Max);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void GetStudentStats_WithEmptyAttempts_ReturnsDefaults()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            json.Save("attempts.json", new List<Attempt>());
            var scores = new ScoreService(json);
            var stats = new StatsService(json, scores);

            var s = stats.GetStudentStats("nobody");
            Assert.Equal(0, s.TotalQuestions);
            Assert.Equal(0, s.CorrectAnswers);
            Assert.Equal(0, s.AccuracyRate);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void GetTeacherStats_WithEmptyData_DoesNotBreak()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            json.Save("attempts.json", new List<Attempt>());
            var scores = new ScoreService(json);
            var stats = new StatsService(json, scores);

            var t = stats.GetTeacherStats();
            Assert.Equal(0, t.TotalQuestions);
            Assert.Empty(t.Ranking);
            Assert.Empty(t.RecentAttempts);
            Assert.NotNull(t.Quartiles);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void GetTeacherStats_WithMissingFile_DoesNotBreak()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var scores = new ScoreService(json);
            var stats = new StatsService(json, scores);

            var t = stats.GetTeacherStats();
            Assert.Equal(0, t.TotalQuestions);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void ResponseTime_ZeroValues_AreIgnored()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var attempts = new List<Attempt>
            {
                new() { Username = "u1", Level = 1, Topic = "IPv4", IsCorrect = true, Points = 10, ResponseTimeSeconds = 0, SessionId = "s1", ScoreAfterAttempt = 10, CreatedAt = DateTime.UtcNow },
                new() { Username = "u1", Level = 1, Topic = "IPv4", IsCorrect = true, Points = 10, ResponseTimeSeconds = 2.0, SessionId = "s2", ScoreAfterAttempt = 20, CreatedAt = DateTime.UtcNow },
                new() { Username = "u1", Level = 1, Topic = "IPv4", IsCorrect = false, Points = -5, ResponseTimeSeconds = 0, SessionId = "s3", ScoreAfterAttempt = 15, CreatedAt = DateTime.UtcNow },
            };
            json.Save("attempts.json", attempts);
            var scores = new ScoreService(json);
            scores.AddPoints("u1", 15);
            var stats = new StatsService(json, scores);

            var s = stats.GetStudentStats("u1");
            // Only 1 non-zero response time, average should be 2.0
            Assert.True(s.AverageResponseTime >= 1.9 && s.AverageResponseTime <= 2.1);
        }
        finally { TestHelpers.Cleanup(dir); }
    }
}
