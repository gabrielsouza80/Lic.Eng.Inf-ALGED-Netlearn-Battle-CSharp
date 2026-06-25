using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetLearnBattle.CSharp.Models;
using NetLearnBattle.CSharp.Services;

namespace NetLearnBattle.CSharp.Pages;

public class StatsModel : PageModel
{
    private readonly StatsService _stats;
    private readonly ScoreService _scores;

    public StatsModel(StatsService stats, ScoreService scores)
    {
        _stats = stats;
        _scores = scores;
    }

    public StudentStats? StudentStats { get; set; }

    public IActionResult OnGet()
    {
        // [M29] Estatísticas são calculadas para o aluno autenticado.
        var username = HttpContext.Session.GetString("Username");
        if (string.IsNullOrEmpty(username))
            return RedirectToPage("/Login");

        StudentStats = _stats.GetStudentStats(username);
        return Page();
    }
}
