using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetLearnBattle.CSharp.Services;

namespace NetLearnBattle.CSharp.Pages;

public class RegisterModel : PageModel
{
    private readonly AuthService _auth;

    public RegisterModel(AuthService auth)
    {
        _auth = auth;
    }

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }

    public IActionResult OnPost()
    {
        // [M21] Registo delega validação e hash ao AuthService.
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            Message = "Preenche todos os campos.";
            return Page();
        }

        var result = _auth.Register(Username, Password);

        if (result)
        {
            Message = "Conta criada com sucesso! Podes fazer login.";
            IsSuccess = true;
        }
        else
        {
            Message = "Esse nome de utilizador já existe ou é inválido.";
        }

        return Page();
    }
}
