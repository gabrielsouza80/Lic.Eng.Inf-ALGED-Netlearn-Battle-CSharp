using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetLearnBattle.CSharp.Models;
using NetLearnBattle.CSharp.Services;

namespace NetLearnBattle.CSharp.Pages;

public class RankingModel : PageModel
{
    private readonly ScoreService _scores;

    public RankingModel(ScoreService scores)
    {
        _scores = scores;
    }

    public List<ScoreEntry> Ranking { get; set; } = new();

    public IActionResult OnGet()
    {
        Ranking = _scores.GetRanking();
        return Page();
    }
}
