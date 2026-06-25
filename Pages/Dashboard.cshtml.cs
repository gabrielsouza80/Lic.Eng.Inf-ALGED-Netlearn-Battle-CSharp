using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetLearnBattle.CSharp.Services;

namespace NetLearnBattle.CSharp.Pages;

public class DashboardModel : PageModel
{
    private readonly ScoreService _scores;

    public DashboardModel(ScoreService scores)
    {
        _scores = scores;
    }

    public string Username { get; set; } = string.Empty;
    public int Score { get; set; }

    public IActionResult OnGet()
    {
        // [M24] Dashboard exige login e mostra score atual.
        Username = HttpContext.Session.GetString("Username") ?? string.Empty;

        if (string.IsNullOrEmpty(Username))
            return RedirectToPage("/Login");

        Score = _scores.GetScore(Username);
        return Page();
    }
}
