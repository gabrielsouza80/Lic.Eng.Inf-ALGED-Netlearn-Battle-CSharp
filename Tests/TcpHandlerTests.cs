using System.Text.Json;
using NetLearnBattle.CSharp.Network;

namespace NetLearnBattle.CSharp.Tests;

// [M37] Testes das mensagens TCP.
public class TcpHandlerTests
{
    private (string dir, JsonService json, AuthService auth, ScoreService scores, StatsService stats, IpService ip, AclService acl, GameService game, GameSessionStore store, TcpServer server) Setup()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        var auth = new AuthService(json);
        auth.Register("tcpuser", "pass123");
        var scores = new ScoreService(json);
        var store = new GameSessionStore();
        var ip = new IpService();
        var acl = new AclService(json);
        var game = new GameService(json, scores, store, ip, acl);
        var stats = new StatsService(json, scores);
        var server = new TcpServer(auth, scores, stats, ip, acl, game, json, 5001);
        return (dir, json, auth, scores, stats, ip, acl, game, store, server);
    }

    private static JsonElement Parse(string json)
    {
        return JsonDocument.Parse(json).RootElement;
    }

    private static string GetResponseType(string responseJson)
    {
        var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement.GetProperty("type").GetString() ?? "";
    }

    private static bool GetSuccess(string responseJson)
    {
        var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean();
    }

    private static string GetMessage(string responseJson)
    {
        var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
    }

    [Fact]
    public async Task AuthRequest_ValidCredentials_ReturnsSuccess()
    {
        var (dir, _, _, _, _, _, _, _, _, server) = Setup();
        try
        {
            var state = new TcpServer.ClientState();
            var root = Parse("{\"type\":\"AUTH_REQUEST\",\"username\":\"tcpuser\",\"password\":\"pass123\"}");
            var response = await server.HandleMessage("AUTH_REQUEST", root, state);
            Assert.Equal("AUTH_RESPONSE", GetResponseType(response));
            Assert.True(GetSuccess(response));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public async Task AuthRequest_InvalidCredentials_ReturnsFailure()
    {
        var (dir, _, _, _, _, _, _, _, _, server) = Setup();
        try
        {
            var state = new TcpServer.ClientState();
            var root = Parse("{\"type\":\"AUTH_REQUEST\",\"username\":\"tcpuser\",\"password\":\"wrong\"}");
            var response = await server.HandleMessage("AUTH_REQUEST", root, state);
            Assert.Equal("AUTH_RESPONSE", GetResponseType(response));
            Assert.False(GetSuccess(response));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public async Task QuestionRequest_WithoutAuth_ReturnsError()
    {
        var (dir, _, _, _, _, _, _, _, _, server) = Setup();
        try
        {
            var state = new TcpServer.ClientState();
            var root = Parse("{}");
            var response = await server.HandleMessage("QUESTION_REQUEST", root, state);
            Assert.Equal("ERROR", GetResponseType(response));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public async Task QuestionPush_DoesNotContainCorrectIndex()
    {
        var (dir, _, _, _, _, _, _, _, _, server) = Setup();
        try
        {
            var state = new TcpServer.ClientState();
            state.Username = "tcpuser";
            var root = Parse("{\"type\":\"QUESTION_REQUEST\",\"level\":1}");
            var response = await server.HandleMessage("QUESTION_REQUEST", root, state);
            Assert.Equal("QUESTION_PUSH", GetResponseType(response));
            var doc = JsonDocument.Parse(response);
            Assert.False(doc.RootElement.TryGetProperty("correctIndex", out _));
            Assert.False(doc.RootElement.TryGetProperty("correctindex", out _));
            Assert.False(doc.RootElement.TryGetProperty("correct_index", out _));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public async Task AnswerSubmit_WithoutActiveQuestion_ReturnsError()
    {
        var (dir, _, _, _, _, _, _, _, _, server) = Setup();
        try
        {
            var state = new TcpServer.ClientState();
            state.Username = "tcpuser";
            var root = Parse("{\"type\":\"ANSWER_SUBMIT\",\"selectedIndex\":0}");
            var response = await server.HandleMessage("ANSWER_SUBMIT", root, state);
            Assert.Equal("ERROR", GetResponseType(response));
            Assert.Contains("pergunta ativa", GetMessage(response).ToLowerInvariant());
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public async Task AnswerSubmit_Valid_UpdatesScore()
    {
        var (dir, json, _, scores, _, _, _, _, _, server) = Setup();
        try
        {
            var state = new TcpServer.ClientState();
            state.Username = "tcpuser";
            // Get a question first
            var qRoot = Parse("{\"type\":\"QUESTION_REQUEST\",\"level\":1}");
            var qResponse = await server.HandleMessage("QUESTION_REQUEST", qRoot, state);
            Assert.Equal("QUESTION_PUSH", GetResponseType(qResponse));

            // Now answer with index 0
            var aRoot = Parse("{\"type\":\"ANSWER_SUBMIT\",\"selectedIndex\":0}");
            var aResponse = await server.HandleMessage("ANSWER_SUBMIT", aRoot, state);
            Assert.Equal("ANSWER_RESULT", GetResponseType(aResponse));
            var result = JsonDocument.Parse(aResponse).RootElement;
            var points = result.GetProperty("points").GetInt32();
            Assert.Equal(points, scores.GetScore("tcpuser"));
            Assert.Single(json.LoadList<Attempt>("attempts.json"));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public async Task AnswerSubmit_RepeatedAnswer_DoesNotUpdateScoreAgain()
    {
        var (dir, json, _, scores, _, _, _, _, _, server) = Setup();
        try
        {
            var state = new TcpServer.ClientState { Username = "tcpuser" };
            await server.HandleMessage("QUESTION_REQUEST", Parse("{\"level\":1}"), state);

            var answer = Parse("{\"selectedIndex\":0}");
            await server.HandleMessage("ANSWER_SUBMIT", answer, state);
            var scoreAfterFirstAnswer = scores.GetScore("tcpuser");

            var repeatedResponse = await server.HandleMessage("ANSWER_SUBMIT", answer, state);
            Assert.Equal("ERROR", GetResponseType(repeatedResponse));
            Assert.Equal(scoreAfterFirstAnswer, scores.GetScore("tcpuser"));
            Assert.Single(json.LoadList<Attempt>("attempts.json"));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public async Task ScoreUpdate_ReturnsScore()
    {
        var (dir, _, _, scores, _, _, _, _, _, server) = Setup();
        try
        {
            scores.AddPoints("tcpuser", 75);
            var state = new TcpServer.ClientState();
            state.Username = "tcpuser";
            var root = Parse("{}");
            var response = await server.HandleMessage("SCORE_UPDATE", root, state);
            Assert.Equal("SCORE_UPDATE", GetResponseType(response));
            var doc = JsonDocument.Parse(response);
            var score = doc.RootElement.GetProperty("score").GetInt32();
            Assert.Equal(75, score);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public async Task RankingRequest_ReturnsRanking()
    {
        var (dir, _, _, scores, _, _, _, _, _, server) = Setup();
        try
        {
            scores.AddPoints("user1", 100);
            scores.AddPoints("user2", 200);
            var state = new TcpServer.ClientState();
            var root = Parse("{}");
            var response = await server.HandleMessage("RANKING_REQUEST", root, state);
            Assert.Equal("RANKING_RESPONSE", GetResponseType(response));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public async Task StatsRequest_RequiresAuth()
    {
        var (dir, _, _, _, _, _, _, _, _, server) = Setup();
        try
        {
            var state = new TcpServer.ClientState();
            var root = Parse("{}");
            var response = await server.HandleMessage("STATS_REQUEST", root, state);
            Assert.Equal("ERROR", GetResponseType(response));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public async Task StatsRequest_WithAuth_ReturnsStats()
    {
        var (dir, _, _, _, _, _, _, _, _, server) = Setup();
        try
        {
            var state = new TcpServer.ClientState();
            state.Username = "tcpuser";
            var root = Parse("{}");
            var response = await server.HandleMessage("STATS_REQUEST", root, state);
            Assert.Equal("STATS_RESPONSE", GetResponseType(response));
            var doc = JsonDocument.Parse(response);
            Assert.True(doc.RootElement.TryGetProperty("total", out _));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public async Task EndSession_ReturnsEndMessage()
    {
        var (dir, _, _, _, _, _, _, _, _, server) = Setup();
        try
        {
            var state = new TcpServer.ClientState();
            var root = Parse("{}");
            var response = await server.HandleMessage("END_SESSION", root, state);
            Assert.Equal("END_SESSION", GetResponseType(response));
            Assert.Contains("terminada", GetMessage(response).ToLowerInvariant());
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public async Task UnknownType_ReturnsError()
    {
        var (dir, _, _, _, _, _, _, _, _, server) = Setup();
        try
        {
            var state = new TcpServer.ClientState();
            var root = Parse("{}");
            var response = await server.HandleMessage("UNKNOWN_TYPE", root, state);
            Assert.Equal("ERROR", GetResponseType(response));
            Assert.Contains("desconhecido", GetMessage(response).ToLowerInvariant());
        }
        finally { TestHelpers.Cleanup(dir); }
    }
}
