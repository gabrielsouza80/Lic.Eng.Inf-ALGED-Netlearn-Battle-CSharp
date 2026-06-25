using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetLearnBattle.CSharp.Models;
using NetLearnBattle.CSharp.Services;

namespace NetLearnBattle.CSharp.Pages;

public class SummaryModel : PageModel
{
    private readonly GameSessionStore _store;
    private readonly ScoreService _scores;

    public SummaryModel(GameSessionStore store, ScoreService scores)
    {
        _store = store;
        _scores = scores;
    }

    public GameSession? Session { get; set; }
    public int FinalScore { get; set; }

    public IActionResult OnGet()
    {
        // [M27] Summary tenta carregar a sessão ativa ou a última sessão respondida.
        var username = HttpContext.Session.GetString("Username");
        if (string.IsNullOrEmpty(username))
            return RedirectToPage("/Login");

        var sessionId = HttpContext.Session.GetString("GameSessionId");

        if (!string.IsNullOrEmpty(sessionId))
        {
            Session = _store.GetSession(sessionId);
            if (Session != null && Session.Username == username)
            {
                FinalScore = _scores.GetScore(username);
                HttpContext.Session.Remove("GameSessionId");
                return Page();
            }
        }

        sessionId = HttpContext.Session.GetString("LastAttemptSessionId");
        if (!string.IsNullOrEmpty(sessionId))
        {
            Session = _store.GetSession(sessionId);
            if (Session != null && Session.Username == username)
            {
                FinalScore = _scores.GetScore(username);
                return Page();
            }
        }

        return Page();
    }
}
