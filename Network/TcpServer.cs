using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using NetLearnBattle.CSharp.Models;
using NetLearnBattle.CSharp.Services;

namespace NetLearnBattle.CSharp.Network;

public class TcpServer
{
    private readonly AuthService _auth;
    private readonly ScoreService _scores;
    private readonly StatsService _stats;
    private readonly IpService _ip;
    private readonly AclService _acl;
    private readonly GameService _game;
    private readonly JsonService _json;
    private readonly string _host;
    private readonly int _port;

    internal class ClientState
    {
        public string? Username { get; set; }
        public Question? ActiveQuestion { get; set; }
        public DateTime? QuestionStartedAt { get; set; }
        public bool IsAuthenticated => !string.IsNullOrEmpty(Username);
    }

    public TcpServer(AuthService auth, ScoreService scores, StatsService stats,
        IpService ip, AclService acl, GameService game, JsonService json, int port = 5001,
        string host = "127.0.0.1")
    {
        _auth = auth;
        _scores = scores;
        _stats = stats;
        _ip = ip;
        _acl = acl;
        _game = game;
        _json = json;
        _port = port;
        _host = host;
    }

    public async Task StartAsync()
    {
        var address = IPAddress.TryParse(_host, out var parsedAddress)
            ? parsedAddress : IPAddress.Any;
        var listener = new TcpListener(address, _port);
        listener.Start();
        Console.WriteLine($"TCP Server a aguardar ligações em {_host}:{_port}...");

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            Console.WriteLine("Cliente ligado.");
            await HandleClientAsync(client);
            Console.WriteLine("Cliente desligado.");
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        var state = new ClientState();
        var stream = client.GetStream();
        var reader = new StreamReader(stream, Encoding.UTF8);
        var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true, NewLine = "\n" };

        try
        {
            while (client.Connected)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break;

                JsonDocument? doc = null;
                try
                {
                    doc = JsonDocument.Parse(line);
                }
                catch
                {
                    await SendError(writer, "JSON inválido.");
                    continue;
                }

                var root = doc.RootElement;
                if (!root.TryGetProperty("type", out var typeProp))
                {
                    await SendError(writer, "Campo 'type' ausente.");
                    continue;
                }

                var type = typeProp.GetString() ?? string.Empty;
                var response = await HandleMessage(type, root, state);
                await writer.WriteLineAsync(response);
            }
        }
        catch (Exception ex)
        {
            try { await SendError(writer, $"Erro: {ex.Message}"); } catch { }
        }
    }

    internal Task<string> HandleMessage(string type, JsonElement root, ClientState state)
    {
        return type switch
        {
            "AUTH_REQUEST" => HandleAuth(root, state),
            "QUESTION_REQUEST" => HandleQuestionRequest(root, state),
            "ANSWER_SUBMIT" => HandleAnswerSubmit(root, state),
            "SCORE_UPDATE" => HandleScoreUpdate(state),
            "RANKING_REQUEST" => HandleRankingRequest(),
            "STATS_REQUEST" => HandleStatsRequest(state),
            "END_SESSION" => HandleEndSession(),
            _ => Task.FromResult(CreateJson("ERROR", new Dictionary<string, object> { ["message"] = $"Tipo desconhecido: {type}" })),
        };
    }

    private Task<string> HandleAuth(JsonElement root, ClientState state)
    {
        var username = GetString(root, "username");
        var password = GetString(root, "password");

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return Task.FromResult(CreateJson("AUTH_RESPONSE", new Dictionary<string, object> { ["success"] = false }));
        }

        var user = _auth.Login(username, password);
        if (user != null)
        {
            state.Username = user.Username;
            return Task.FromResult(CreateJson("AUTH_RESPONSE", new Dictionary<string, object> { ["success"] = true }));
        }

        return Task.FromResult(CreateJson("AUTH_RESPONSE", new Dictionary<string, object> { ["success"] = false }));
    }

    private Task<string> HandleQuestionRequest(JsonElement root, ClientState state)
    {
        if (!state.IsAuthenticated)
        {
            return Task.FromResult(CreateJson("ERROR", new Dictionary<string, object> { ["message"] = "Autenticação necessária." }));
        }

        var level = GetInt(root, "level", 1);

        Question question;
        try
        {
            question = level switch
            {
                1 => _ip.GenerateIpv4Question(1),
                2 => _ip.GenerateIpv4Question(2),
                3 => _ip.GenerateIpv4Question(3),
                4 => _ip.GenerateIpv6Question(),
                5 => _acl.GenerateAclQuestion(),
                _ => _ip.GenerateIpv4Question(1),
            };
        }
        catch
        {
            return Task.FromResult(CreateJson("ERROR", new Dictionary<string, object> { ["message"] = "Erro ao gerar pergunta." }));
        }

        state.ActiveQuestion = question;
        state.QuestionStartedAt = DateTime.UtcNow;

        return Task.FromResult(CreateJson("QUESTION_PUSH", new Dictionary<string, object>
        {
            ["questionId"] = question.Id,
            ["level"] = question.Level,
            ["topic"] = question.Topic,
            ["questionType"] = question.QuestionType,
            ["question"] = question.QuestionText,
            ["options"] = question.Options,
        }));
    }

    private Task<string> HandleAnswerSubmit(JsonElement root, ClientState state)
    {
        if (!state.IsAuthenticated)
        {
            return Task.FromResult(CreateJson("ERROR", new Dictionary<string, object> { ["message"] = "Autenticação necessária." }));
        }

        if (state.ActiveQuestion == null)
        {
            return Task.FromResult(CreateJson("ERROR", new Dictionary<string, object> { ["message"] = "Não existe pergunta ativa." }));
        }

        var question = state.ActiveQuestion;
        var selectedIndex = GetInt(root, "selectedIndex", -1);

        if (selectedIndex < 0 || selectedIndex >= question.Options.Count)
        {
            return Task.FromResult(CreateJson("ERROR", new Dictionary<string, object> { ["message"] = "selectedIndex inválido." }));
        }

        var isCorrect = selectedIndex == question.CorrectIndex;
        var points = isCorrect ? question.PointsCorrect : question.PointsWrong;
        var responseTime = 0.0;

        if (state.QuestionStartedAt.HasValue)
        {
            responseTime = (DateTime.UtcNow - state.QuestionStartedAt.Value).TotalSeconds;
        }

        _scores.AddPoints(state.Username!, points);
        var currentScore = _scores.GetScore(state.Username!);

        var attempt = new Attempt
        {
            Username = state.Username!,
            Level = question.Level,
            Topic = question.Topic,
            Question = question.QuestionText,
            SelectedAnswer = selectedIndex >= 0 && selectedIndex < question.Options.Count
                ? question.Options[selectedIndex] : string.Empty,
            CorrectAnswer = question.CorrectAnswer,
            SelectedIndex = selectedIndex,
            CorrectIndex = question.CorrectIndex,
            IsCorrect = isCorrect,
            Points = points,
            ResponseTimeSeconds = responseTime,
            SessionId = $"tcp_{Guid.NewGuid():N}",
            ScoreAfterAttempt = currentScore,
            CreatedAt = DateTime.UtcNow,
        };

        var attempts = _json.LoadList<Attempt>("attempts.json");
        attempts.Add(attempt);
        _json.Save("attempts.json", attempts);

        state.ActiveQuestion = null;
        state.QuestionStartedAt = null;

        return Task.FromResult(CreateJson("ANSWER_RESULT", new Dictionary<string, object>
        {
            ["isCorrect"] = isCorrect,
            ["points"] = points,
            ["correctAnswer"] = question.CorrectAnswer,
            ["score"] = currentScore,
        }));
    }

    private Task<string> HandleScoreUpdate(ClientState state)
    {
        if (!state.IsAuthenticated)
        {
            return Task.FromResult(CreateJson("ERROR", new Dictionary<string, object> { ["message"] = "Autenticação necessária." }));
        }

        var score = _scores.GetScore(state.Username!);
        return Task.FromResult(CreateJson("SCORE_UPDATE", new Dictionary<string, object> { ["score"] = score }));
    }

    private Task<string> HandleRankingRequest()
    {
        var ranking = _scores.GetRanking();
        return Task.FromResult(CreateJson("RANKING_RESPONSE", new Dictionary<string, object> { ["ranking"] = ranking }));
    }

    private Task<string> HandleStatsRequest(ClientState state)
    {
        if (!state.IsAuthenticated)
        {
            return Task.FromResult(CreateJson("ERROR", new Dictionary<string, object> { ["message"] = "Autenticação necessária." }));
        }

        var s = _stats.GetStudentStats(state.Username!);
        return Task.FromResult(CreateJson("STATS_RESPONSE", new Dictionary<string, object>
        {
            ["total"] = s.TotalQuestions,
            ["correct"] = s.CorrectAnswers,
            ["wrong"] = s.WrongAnswers,
            ["accuracyRate"] = Math.Round(s.AccuracyRate, 1),
            ["currentScore"] = s.CurrentScore,
            ["weakestTopic"] = s.WeakestTopic,
        }));
    }

    private Task<string> HandleEndSession()
    {
        return Task.FromResult(CreateJson("END_SESSION", new Dictionary<string, object>
        {
            ["message"] = "Sessão terminada pelo servidor.",
        }));
    }

    private static async Task SendError(StreamWriter writer, string message)
    {
        try
        {
            await writer.WriteLineAsync(CreateJson("ERROR", new Dictionary<string, object>
            {
                ["message"] = message,
            }));
        }
        catch { }
    }

    private static string GetString(JsonElement root, string key)
    {
        return root.TryGetProperty(key, out var prop) ? prop.GetString() ?? string.Empty : string.Empty;
    }

    private static int GetInt(JsonElement root, string key, int defaultValue)
    {
        return root.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.Number
            ? prop.GetInt32() : defaultValue;
    }

    private static string CreateJson(string type, Dictionary<string, object> fields)
    {
        var dict = new Dictionary<string, object> { ["type"] = type };
        foreach (var kv in fields)
            dict[kv.Key] = kv.Value;
        return JsonSerializer.Serialize(dict, new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        });
    }
}
