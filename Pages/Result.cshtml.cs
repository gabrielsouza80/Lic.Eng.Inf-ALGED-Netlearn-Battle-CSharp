using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetLearnBattle.CSharp.Models;
using NetLearnBattle.CSharp.Services;

namespace NetLearnBattle.CSharp.Pages;

public class ResultModel : PageModel
{
    private readonly GameSessionStore _store;
    private readonly ScoreService _scores;

    public ResultModel(GameSessionStore store, ScoreService scores)
    {
        _store = store;
        _scores = scores;
    }

    public Attempt? Attempt { get; set; }
    public bool HasNextQuestion { get; set; }

    public IActionResult OnGet()
    {
        // [M26] Result mostra apenas o último feedback da sessão do utilizador.
        var username = HttpContext.Session.GetString("Username");
        if (string.IsNullOrEmpty(username))
            return RedirectToPage("/Login");

        var sessionId = HttpContext.Session.GetString("LastAttemptSessionId");

        if (string.IsNullOrEmpty(sessionId))
            return RedirectToPage("/Play");

        var session = _store.GetSession(sessionId);
        if (session == null || session.Username != username)
            return RedirectToPage("/Play");

        Attempt = session.Attempts.LastOrDefault();
        HasNextQuestion = !session.IsFinished;

        return Page();
    }
}
