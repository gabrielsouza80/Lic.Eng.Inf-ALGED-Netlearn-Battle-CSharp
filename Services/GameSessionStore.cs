using System.Collections.Concurrent;
using NetLearnBattle.CSharp.Models;

namespace NetLearnBattle.CSharp.Services;

public class GameSessionStore
{
    // [M14] Sessões temporárias ficam em memória durante a execução.
    private readonly ConcurrentDictionary<string, GameSession> _sessions = new();

    public GameSession CreateSession(string username, int level)
    {
        // [M14] Cada sessão fica associada ao dono pelo Username.
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
