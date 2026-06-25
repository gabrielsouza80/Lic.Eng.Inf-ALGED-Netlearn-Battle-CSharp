using System.Collections.Concurrent;
using NetLearnBattle.CSharp.Models;

namespace NetLearnBattle.CSharp.Services;

public class GameSessionStore
{
    private readonly ConcurrentDictionary<string, GameSession> _sessions = new();

    public GameSession CreateSession(string username, int level)
    {
        var session = new GameSession
        {
            Username = username,
            Level = level,
            StartedAt = DateTime.UtcNow
        };

        _sessions[session.SessionId] = session;
        return session;
    }

    public GameSession? GetSession(string? sessionId)
    {
        if (string.IsNullOrEmpty(sessionId)) return null;
        return _sessions.GetValueOrDefault(sessionId);
    }

    public void UpdateSession(GameSession session)
    {
        _sessions[session.SessionId] = session;
    }

    public void RemoveSession(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
    }
}
