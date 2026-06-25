using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace NetLearnBattle.CSharp.Network;

public class TcpClientDemo
{
    private readonly string _host;
    private readonly int _port;

    public TcpClientDemo(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public async Task RunAsync()
    {
        Console.WriteLine("=== NetLearn Battle — TCP Client ===\n");

        try
        {
            using var client = new TcpClient(_host, _port);
            var stream = client.GetStream();
            var reader = new StreamReader(stream, Encoding.UTF8);
            var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true, NewLine = "\n" };

            Console.WriteLine($"Ligado a {_host}:{_port}\n");

            // 1. AUTH_REQUEST
            Console.Write("Username: ");
            var username = Console.ReadLine() ?? string.Empty;
            Console.Write("Password: ");
            var password = ReadPassword();

            await SendMessage(writer, new { type = "AUTH_REQUEST", username, password });
            var response = await ReadMessage(reader);
            Console.WriteLine($"\nAUTH_RESPONSE: {response}");

            var authDoc = JsonDocument.Parse(response);
            var authSuccess = authDoc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean();
            if (!authSuccess)
            {
                Console.WriteLine("Autenticação falhou. A encerrar.");
                return;
            }
            Console.WriteLine("Autenticação bem-sucedida!\n");

            // 2. QUESTION_REQUEST
            Console.Write("Nível (1-5, padrão 1): ");
            var levelInput = Console.ReadLine();
            var level = int.TryParse(levelInput, out var l) && l >= 1 && l <= 5 ? l : 1;

            await SendMessage(writer, new { type = "QUESTION_REQUEST", level });
            var questionResponse = await ReadMessage(reader);
            Console.WriteLine($"\n{questionResponse}");

            var qDoc = JsonDocument.Parse(questionResponse);
            if (qDoc.RootElement.TryGetProperty("type", out var qType) && qType.GetString() == "QUESTION_PUSH")
            {
                ShowQuestion(qDoc.RootElement);

                // 3. ANSWER_SUBMIT
                Console.Write("\nA tua resposta (número da opção): ");
                var selectedInput = Console.ReadLine();
                var selectedIndex = int.TryParse(selectedInput, out var si) ? si : 0;

                await SendMessage(writer, new { type = "ANSWER_SUBMIT", selectedIndex });
                var answerResult = await ReadMessage(reader);
                Console.WriteLine($"\n{answerResult}");
            }

            // 4. SCORE_UPDATE
            await SendMessage(writer, new { type = "SCORE_UPDATE" });
            var scoreResponse = await ReadMessage(reader);
            Console.WriteLine($"\n{scoreResponse}");

            // 5. RANKING_REQUEST
            await SendMessage(writer, new { type = "RANKING_REQUEST" });
            var rankingResponse = await ReadMessage(reader);
            Console.WriteLine($"\n{rankingResponse}");

            // 6. STATS_REQUEST
            await SendMessage(writer, new { type = "STATS_REQUEST" });
            var statsResponse = await ReadMessage(reader);
            Console.WriteLine($"\n{statsResponse}");

            // 7. END_SESSION
            await SendMessage(writer, new { type = "END_SESSION" });
            var endResponse = await ReadMessage(reader);
            Console.WriteLine($"\n{endResponse}");

            Console.WriteLine("\n=== Sessão TCP concluída ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro de ligação: {ex.Message}");
        }
    }

    private static void ShowQuestion(JsonElement root)
    {
        Console.WriteLine("\n--- PERGUNTA ---");
        if (root.TryGetProperty("question", out var q))
            Console.WriteLine($"Pergunta: {q.GetString()}");

        if (root.TryGetProperty("level", out var l))
            Console.WriteLine($"Nível: {l.GetInt32()}");

        if (root.TryGetProperty("topic", out var t))
            Console.WriteLine($"Tópico: {t.GetString()}");

        if (root.TryGetProperty("options", out var opts) && opts.ValueKind == JsonValueKind.Array)
        {
            Console.WriteLine("\nOpções:");
            var i = 0;
            foreach (var opt in opts.EnumerateArray())
            {
                Console.WriteLine($"  {i}: {opt.GetString()}");
                i++;
            }
        }

        Console.WriteLine("----------------");
    }

    private static async Task SendMessage(StreamWriter writer, object message)
    {
        var json = JsonSerializer.Serialize(message);
        await writer.WriteLineAsync(json);
    }

    private static async Task<string> ReadMessage(StreamReader reader)
    {
        var line = await reader.ReadLineAsync();
        return line ?? string.Empty;
    }

    private static string ReadPassword()
    {
        var password = string.Empty;
        ConsoleKeyInfo key;
        do
        {
            key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password = password[..^1];
                Console.Write("\b \b");
            }
            else if (key.Key != ConsoleKey.Enter && key.Key != ConsoleKey.Backspace)
            {
                password += key.KeyChar;
                Console.Write('*');
            }
        } while (key.Key != ConsoleKey.Enter);
        Console.WriteLine();
        return password;
    }
}
