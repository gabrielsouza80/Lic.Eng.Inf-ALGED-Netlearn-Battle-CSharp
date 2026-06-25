namespace NetLearnBattle.CSharp.Models;

// [M06] Utilizador guardado com hash e salt, sem password em texto simples.
public class User
{
    public string Username { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
}
