using NetLearnBattle.CSharp.Models;

namespace NetLearnBattle.CSharp.Services;

public class ScoreService
{
    private const string ScoresFile = "scores.json";
    private readonly JsonService _json;

    public ScoreService(JsonService json)
    {
        _json = json;
    }

    public int GetScore(string username)
    {
        var scores = _json.LoadDictionary<int>(ScoresFile);
        return scores.GetValueOrDefault(username, 0);
    }

    public void AddPoints(string username, int points)
    {
        var scores = _json.LoadDictionary<int>(ScoresFile);
        var current = scores.GetValueOrDefault(username, 0);
        scores[username] = current + points;
        _json.Save(ScoresFile, scores);
    }

    public List<ScoreEntry> GetRanking()
    {
        var scores = GetAllScores();
        return scores
            .Select(kv => new ScoreEntry { Username = kv.Key, Score = kv.Value })
            .OrderByDescending(s => s.Score)
            .Take(5)
            .ToList();
    }

    public Dictionary<string, int> GetAllScores()
    {
        return _json.LoadDictionary<int>(ScoresFile);
    }
}
