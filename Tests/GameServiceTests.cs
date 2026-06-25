namespace NetLearnBattle.CSharp.Tests;

public class GameServiceTests
{
    [Fact]
    public void StartSession_CreatesSessionWith5Questions()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var scores = new ScoreService(json);
            var store = new GameSessionStore();
            var ip = new IpService();
            var acl = new AclService(json);
            var game = new GameService(json, scores, store, ip, acl);

            var session = game.StartSession("testuser", 1);
            Assert.Equal(5, session.TotalQuestions);
            Assert.NotNull(session.CurrentQuestion);
            Assert.Equal("testuser", session.Username);
            Assert.Equal(1, session.Level);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void StartSession_UsesQueue()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var scores = new ScoreService(json);
            var store = new GameSessionStore();
            var ip = new IpService();
            var acl = new AclService(json);
            var game = new GameService(json, scores, store, ip, acl);

            var session = game.StartSession("queueuser", 1);
            Assert.Equal(4, session.QuestionsRemaining);
            Assert.IsAssignableFrom<Queue<NetLearnBattle.CSharp.Models.Question>>(session.Questions);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void Level1_GeneratesIpv4Questions()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var scores = new ScoreService(json);
            var store = new GameSessionStore();
            var ip = new IpService();
            var acl = new AclService(json);
            var game = new GameService(json, scores, store, ip, acl);

            var session = game.StartSession("lvl1user", 1);
            Assert.Contains("IPv4", session.CurrentQuestion.Topic, StringComparison.OrdinalIgnoreCase);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void Level4_GeneratesIpv6Questions()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var scores = new ScoreService(json);
            var store = new GameSessionStore();
            var ip = new IpService();
            var acl = new AclService(json);
            var game = new GameService(json, scores, store, ip, acl);

            var session = game.StartSession("lvl4user", 4);
            Assert.Equal("IPv6", session.CurrentQuestion.Topic);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void Level5_GeneratesAclQuestions()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        SeedAclsJson(dir);
        try
        {
            var scores = new ScoreService(json);
            var store = new GameSessionStore();
            var ip = new IpService();
            var acl = new AclService(json);
            var game = new GameService(json, scores, store, ip, acl);

            var session = game.StartSession("lvl5user", 5);
            Assert.Contains("ACL", session.CurrentQuestion.Topic, StringComparison.OrdinalIgnoreCase);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void CorrectAnswer_AddsPositivePoints()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var scores = new ScoreService(json);
            var store = new GameSessionStore();
            var ip = new IpService();
            var acl = new AclService(json);
            var game = new GameService(json, scores, store, ip, acl);

            var session = game.StartSession("correctuser", 1);
            var correctIndex = session.CurrentQuestion.CorrectIndex;
            var pointsBefore = scores.GetScore("correctuser");

            var attempt = game.GradeAnswer(session, correctIndex);
            Assert.True(attempt.IsCorrect);
            Assert.True(attempt.Points > 0);
            Assert.Equal(pointsBefore + attempt.Points, scores.GetScore("correctuser"));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void WrongAnswer_SubtractsPoints()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var scores = new ScoreService(json);
            var store = new GameSessionStore();
            var ip = new IpService();
            var acl = new AclService(json);
            var game = new GameService(json, scores, store, ip, acl);

            var session = game.StartSession("wronguser", 1);
            var wrongIndex = (session.CurrentQuestion.CorrectIndex + 1) % session.CurrentQuestion.Options.Count;
            var pointsBefore = scores.GetScore("wronguser");

            var attempt = game.GradeAnswer(session, wrongIndex);
            Assert.False(attempt.IsCorrect);
            Assert.True(attempt.Points < 0);
            Assert.Equal(pointsBefore + attempt.Points, scores.GetScore("wronguser"));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void InvalidSelectedIndex_DoesNotUpdateScoreOrSaveAttempt()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var scores = new ScoreService(json);
            var store = new GameSessionStore();
            var ip = new IpService();
            var acl = new AclService(json);
            var game = new GameService(json, scores, store, ip, acl);

            var session = game.StartSession("invalidindex", 1);

            Assert.Throws<ArgumentOutOfRangeException>(() => game.GradeAnswer(session, 99));
            Assert.Equal(0, scores.GetScore("invalidindex"));
            Assert.Empty(json.LoadList<NetLearnBattle.CSharp.Models.Attempt>("attempts.json"));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void Attempt_ContainsSessionId()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var scores = new ScoreService(json);
            var store = new GameSessionStore();
            var ip = new IpService();
            var acl = new AclService(json);
            var game = new GameService(json, scores, store, ip, acl);

            var session = game.StartSession("siduser", 1);
            var attempt = game.GradeAnswer(session, session.CurrentQuestion.CorrectIndex);
            Assert.Equal(session.SessionId, attempt.SessionId);
            Assert.NotEmpty(attempt.SessionId);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void Attempt_ContainsScoreAfterAttempt()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var scores = new ScoreService(json);
            var store = new GameSessionStore();
            var ip = new IpService();
            var acl = new AclService(json);
            var game = new GameService(json, scores, store, ip, acl);

            var session = game.StartSession("scoreafter", 1);
            var attempt = game.GradeAnswer(session, session.CurrentQuestion.CorrectIndex);
            Assert.Equal(scores.GetScore("scoreafter"), attempt.ScoreAfterAttempt);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void Attempt_ContainsResponseTime()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var scores = new ScoreService(json);
            var store = new GameSessionStore();
            var ip = new IpService();
            var acl = new AclService(json);
            var game = new GameService(json, scores, store, ip, acl);

            var session = game.StartSession("rtuser", 1);
            var attempt = game.GradeAnswer(session, session.CurrentQuestion.CorrectIndex);
            Assert.True(attempt.ResponseTimeSeconds >= 0);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void After5Questions_SessionEnds()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var scores = new ScoreService(json);
            var store = new GameSessionStore();
            var ip = new IpService();
            var acl = new AclService(json);
            var game = new GameService(json, scores, store, ip, acl);

            var session = game.StartSession("enduser", 1);
            for (int i = 0; i < 5; i++)
            {
                var idx = session.CurrentQuestion!.CorrectIndex;
                game.GradeAnswer(session, idx);
            }

            Assert.Null(session.CurrentQuestion);
            Assert.True(session.Questions.Count == 0);
            Assert.True(session.IsFinished);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    private static void SeedAclsJson(string dir)
    {
        var acls = new List<object>
        {
            new
            {
                id = "test1",
                description = "Test scenario",
                packet = new { src_ip = "10.0.0.5", dst_ip = "192.168.1.10", protocol = "tcp", port = 80 },
                rules = new[]
                {
                    new { id = "R1", action = "permit", protocol = "tcp", src = "any", dst = "192.168.1.10/32", port = "80" },
                    new { id = "R2", action = "deny", protocol = "ip", src = "any", dst = "any", port = "any" }
                }
            }
        };
        var json = System.Text.Json.JsonSerializer.Serialize(acls);
        File.WriteAllText(Path.Combine(dir, "acls.json"), json);
    }
}
