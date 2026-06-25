namespace NetLearnBattle.CSharp.Models;

public class User
{
    public string Username { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
}
