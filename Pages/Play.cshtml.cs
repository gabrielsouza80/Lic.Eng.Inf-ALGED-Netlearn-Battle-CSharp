using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetLearnBattle.CSharp.Services;

namespace NetLearnBattle.CSharp.Pages;

public class PlayModel : PageModel
{
    private readonly GameService _game;
    private readonly GameSessionStore _store;

    public PlayModel(GameService game, GameSessionStore store)
    {
        _game = game;
        _store = store;
    }

    public bool HasSession { get; set; }
    public string? SessionId { get; set; }
    public int Level { get; set; }
    public Models.Question? CurrentQuestion { get; set; }
    public int TotalQuestions { get; set; }
    public int CurrentQuestionIndex { get; set; }

    public List<LevelInfo> AvailableLevels { get; set; } = new()
    {
        new(1, "IPv4 básico", "+10 / -5", 10, -5),
        new(2, "Sub-redes IPv4", "+20 / -10", 20, -10),
        new(3, "Super-redes IPv4", "+30 / -15", 30, -15),
        new(4, "IPv6 simples", "+40 / -20", 40, -20),
        new(5, "ACLs", "+50 / -25", 50, -25),
    };

    public IActionResult OnGet()
    {
        // [M25] Play mostra sessão ativa ou escolha de nível.
        var username = HttpContext.Session.GetString("Username");
        if (string.IsNullOrEmpty(username))
            return RedirectToPage("/Login");

        var sessionId = HttpContext.Session.GetString("GameSessionId");

        if (!string.IsNullOrEmpty(sessionId))
        {
            var session = _store.GetSession(sessionId);
            if (session != null && !session.IsFinished)
            {
                HasSession = true;
                SessionId = session.SessionId;
                Level = session.Level;
                CurrentQuestion = session.CurrentQuestion;
                TotalQuestions = session.TotalQuestions;
                CurrentQuestionIndex = session.Attempts.Count + 1;
                return Page();
            }

            HttpContext.Session.Remove("GameSessionId");
        }

        HasSession = false;
        return Page();
    }

    public IActionResult OnPost()
    {
        // [M25] POST inicia nível ou submete resposta da sessão atual.
        var username = HttpContext.Session.GetString("Username");
        if (string.IsNullOrEmpty(username))
            return RedirectToPage("/Login");

        var levelStr = Request.Form["level"].FirstOrDefault();
        var sessionIdStr = Request.Form["sessionId"].FirstOrDefault();
        var selectedStr = Request.Form["selectedIndex"].FirstOrDefault();

        if (!string.IsNullOrEmpty(levelStr) && int.TryParse(levelStr, out var level))
        {
            if (level < 1 || level > 5)
                return RedirectToPage();

            var session = _game.StartSession(username, level);
            HttpContext.Session.SetString("GameSessionId", session.SessionId);
            return RedirectToPage();
        }

        if (!string.IsNullOrEmpty(sessionIdStr) && !string.IsNullOrEmpty(selectedStr)
            && int.TryParse(selectedStr, out var selectedIndex))
        {
            var activeSessionId = HttpContext.Session.GetString("GameSessionId");
            if (!string.Equals(sessionIdStr, activeSessionId, StringComparison.Ordinal))
                return RedirectToPage();

            var session = _store.GetSession(sessionIdStr);
            if (session == null || session.Username != username || session.CurrentQuestion == null)
                return RedirectToPage();
            // [M25] Índice inválido volta para Play sem alterar score.
            if (selectedIndex < 0 || selectedIndex >= session.CurrentQuestion.Options.Count)
                return RedirectToPage();

            var attempt = _game.GradeAnswer(session, selectedIndex);

            HttpContext.Session.SetString("LastAttemptSessionId", session.SessionId);
            return RedirectToPage("/Result");
        }

        return RedirectToPage();
    }

    public record LevelInfo(int Number, string Description, string Points, int PointsCorrect, int PointsWrong)
    {
        public string Name => $"Nível {Number}";
    }
}
