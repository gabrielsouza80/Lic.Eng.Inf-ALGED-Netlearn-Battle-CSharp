using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NetLearnBattle.CSharp.Pages;

public class ErrorModel : PageModel
{
    public void OnGet()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
    }
}
