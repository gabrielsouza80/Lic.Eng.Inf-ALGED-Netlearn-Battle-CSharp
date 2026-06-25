using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetLearnBattle.CSharp.Models;
using NetLearnBattle.CSharp.Services;

namespace NetLearnBattle.CSharp.Pages;

public class TeacherModel : PageModel
{
    private readonly StatsService _stats;

    public TeacherModel(StatsService stats)
    {
        _stats = stats;
    }

    public TeacherStats? TeacherStats { get; set; }

    public IActionResult OnGet()
    {
        TeacherStats = _stats.GetTeacherStats();
        return Page();
    }
}
