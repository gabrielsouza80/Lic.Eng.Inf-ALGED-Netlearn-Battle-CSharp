using System.Security.Cryptography;
using System.Text;
using NetLearnBattle.CSharp.Models;

namespace NetLearnBattle.CSharp.Services;

public class AuthService
{
    private const string UsersFile = "users.json";
    private readonly JsonService _json;

    public AuthService(JsonService json)
    {
        _json = json;
    }

    public bool Register(string username, string password)
    {
        // [M12] Registo cria salt e hash; nunca grava password real.
        var users = _json.LoadList<User>(UsersFile);

        if (users.Any(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            return false;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return false;

        var salt = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var hash = ComputeHash(password, salt);

        users.Add(new User { Username = username, Salt = salt, Hash = hash });
        _json.Save(UsersFile, users);
        return true;
    }

    public User? Login(string username, string password)
    {
        // [M12] Login compara hash calculado com hash guardado.
        var users = _json.LoadList<User>(UsersFile);

        var user = users.FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

        if (user == null) return null;

        var hash = ComputeHash(password, user.Salt);
        return hash == user.Hash ? user : null;
    }

    private static string ComputeHash(string password, string salt)
    {
        // [M41][M42] Salt + password passam por SHA-256.
        var input = Encoding.UTF8.GetBytes(salt + password);
        var hash = SHA256.HashData(input);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
