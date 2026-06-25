using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NetLearnBattle.CSharp.Pages;

public class LogoutModel : PageModel
{
    public IActionResult OnGet()
    {
        // [M23] Limpa toda a sessão do utilizador.
        HttpContext.Session.Clear();
        return Page();
    }
}
