namespace NetLearnBattle.CSharp.Tests;

public class ScoreServiceTests
{
    [Fact]
    public void GetScore_ForUnknownUser_ReturnsZero()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var scores = new ScoreService(json);
            Assert.Equal(0, scores.GetScore("nonexistent"));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void AddPoints_Positive_IncreasesScore()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var scores = new ScoreService(json);
            scores.AddPoints("player1", 50);
            Assert.Equal(50, scores.GetScore("player1"));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void AddPoints_Negative_DecreasesScore()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var scores = new ScoreService(json);
            scores.AddPoints("player2", 100);
            scores.AddPoints("player2", -30);
            Assert.Equal(70, scores.GetScore("player2"));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void AddPoints_MultipleCalls_Accumulates()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var scores = new ScoreService(json);
            scores.AddPoints("player3", 10);
            scores.AddPoints("player3", 20);
            scores.AddPoints("player3", 30);
            Assert.Equal(60, scores.GetScore("player3"));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void GetRanking_ReturnsTop5_OrderedDescending()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var scores = new ScoreService(json);
            scores.AddPoints("p1", 100);
            scores.AddPoints("p2", 200);
            scores.AddPoints("p3", 50);
            scores.AddPoints("p4", 300);
            scores.AddPoints("p5", 150);
            scores.AddPoints("p6", 250);

            var ranking = scores.GetRanking();
            Assert.Equal(5, ranking.Count);
            Assert.Equal(300, ranking[0].Score);
            Assert.Equal(250, ranking[1].Score);
            Assert.Equal(200, ranking[2].Score);
            Assert.Equal(150, ranking[3].Score);
            Assert.Equal(100, ranking[4].Score);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void GetRanking_WithFewerThan5_ReturnsAll()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var scores = new ScoreService(json);
            scores.AddPoints("a", 10);
            scores.AddPoints("b", 20);

            var ranking = scores.GetRanking();
            Assert.Equal(2, ranking.Count);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void GetRanking_Empty_ReturnsEmpty()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var scores = new ScoreService(json);
            var ranking = scores.GetRanking();
            Assert.Empty(ranking);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void Ranking_DoesNotExposeSensitiveData()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var scores = new ScoreService(json);
            scores.AddPoints("alice", 100);
            var ranking = scores.GetRanking();
            var entry = ranking.First();
            Assert.Equal("alice", entry.Username);
            Assert.Equal(100, entry.Score);
            Assert.IsType<int>(entry.Score);
        }
        finally { TestHelpers.Cleanup(dir); }
    }
}
