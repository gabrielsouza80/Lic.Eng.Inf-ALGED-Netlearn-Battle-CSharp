namespace NetLearnBattle.CSharp.Tests;

public class AuthServiceTests
{
    [Fact]
    public void Register_CreatesUser()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var auth = new AuthService(json);
            var result = auth.Register("testuser", "password123");
            Assert.True(result);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void Register_EmptyCredentials_ReturnsFalse()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var auth = new AuthService(json);
            Assert.False(auth.Register("", "pass"));
            Assert.False(auth.Register("user", ""));
            Assert.False(auth.Register("", ""));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void SaltIsGenerated()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var auth = new AuthService(json);
            auth.Register("saltuser", "mypass");
            var users = json.LoadList<NetLearnBattle.CSharp.Models.User>("users.json");
            var user = users.First(u => u.Username == "saltuser");
            Assert.NotNull(user.Salt);
            Assert.NotEmpty(user.Salt);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void HashIsGenerated()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var auth = new AuthService(json);
            auth.Register("hashuser", "mypass");
            var users = json.LoadList<NetLearnBattle.CSharp.Models.User>("users.json");
            var user = users.First(u => u.Username == "hashuser");
            Assert.NotNull(user.Hash);
            Assert.NotEmpty(user.Hash);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void PasswordNotInPlainText()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var auth = new AuthService(json);
            auth.Register("plainuser", "secret123");
            var jsonContent = File.ReadAllText(Path.Combine(dir, "users.json"));
            Assert.DoesNotContain("secret123", jsonContent);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void Login_WithCorrectPassword_Succeeds()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var auth = new AuthService(json);
            auth.Register("logintest", "correct");
            var user = auth.Login("logintest", "correct");
            Assert.NotNull(user);
            Assert.Equal("logintest", user.Username);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void Login_WithWrongPassword_Fails()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var auth = new AuthService(json);
            auth.Register("logintest2", "correct");
            var user = auth.Login("logintest2", "wrong");
            Assert.Null(user);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void Login_WithNonexistentUser_ReturnsNull()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var auth = new AuthService(json);
            var user = auth.Login("nobody", "pass");
            Assert.Null(user);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void DuplicateUsername_IsRejected()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var auth = new AuthService(json);
            Assert.True(auth.Register("dupuser", "pass1"));
            Assert.False(auth.Register("dupuser", "pass2"));
        }
        finally { TestHelpers.Cleanup(dir); }
    }
}
