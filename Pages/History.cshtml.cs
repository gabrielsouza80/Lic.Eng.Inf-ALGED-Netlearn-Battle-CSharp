using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetLearnBattle.CSharp.Models;
using NetLearnBattle.CSharp.Services;

namespace NetLearnBattle.CSharp.Pages;

public class HistoryModel : PageModel
{
    private readonly JsonService _json;

    public HistoryModel(JsonService json)
    {
        _json = json;
    }

    public List<Attempt> Attempts { get; set; } = new();

    public IActionResult OnGet()
    {
        var username = HttpContext.Session.GetString("Username");
        if (string.IsNullOrEmpty(username))
            return RedirectToPage("/Login");

        var allAttempts = _json.LoadList<Attempt>("attempts.json");

        Attempts = allAttempts
            .Where(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(a => a.CreatedAt)
            .ToList();

        return Page();
    }
}
