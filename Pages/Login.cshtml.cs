using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetLearnBattle.CSharp.Services;

namespace NetLearnBattle.CSharp.Pages;

public class LoginModel : PageModel
{
    private readonly AuthService _auth;

    public LoginModel(AuthService auth)
    {
        _auth = auth;
    }

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public IActionResult OnPost()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            Message = "Preenche todos os campos.";
            return Page();
        }

        var user = _auth.Login(Username, Password);

        if (user != null)
        {
            HttpContext.Session.SetString("Username", user.Username);
            return RedirectToPage("/Dashboard");
        }

        Message = "Utilizador ou palavra-passe incorretos.";
        return Page();
    }
}
