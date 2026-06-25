namespace NetLearnBattle.CSharp.Tests;

// [M65] Testes garantem que o reset apaga só dados locais.
public class DataResetServiceTests
{
    [Fact]
    public void Reset_RemovesLocalJsonFiles()
    {
        var dir = CreateTempDataDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "users.json"), "[]");
            File.WriteAllText(Path.Combine(dir, "scores.json"), "{}");
            File.WriteAllText(Path.Combine(dir, "attempts.json"), "[]");
            File.WriteAllText(Path.Combine(dir, "sessions.json"), "[]");

            var service = new DataResetService(dir);
            var removed = service.ResetLocalData();

            Assert.Equal(4, removed.Count);
            Assert.False(File.Exists(Path.Combine(dir, "users.json")));
            Assert.False(File.Exists(Path.Combine(dir, "scores.json")));
            Assert.False(File.Exists(Path.Combine(dir, "attempts.json")));
            Assert.False(File.Exists(Path.Combine(dir, "sessions.json")));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void Reset_DoesNotRemoveFixedProjectData()
    {
        var dir = CreateTempDataDir();
        try
        {
            var examplesDir = Path.Combine(dir, "examples");
            Directory.CreateDirectory(examplesDir);
            File.WriteAllText(Path.Combine(dir, "questions.json"), "[]");
            File.WriteAllText(Path.Combine(dir, "acls.json"), "[]");
            File.WriteAllText(Path.Combine(examplesDir, "users.example.json"), "[]");
            File.WriteAllText(Path.Combine(dir, "users.json"), "[]");

            var service = new DataResetService(dir);
            service.ResetLocalData();

            Assert.True(File.Exists(Path.Combine(dir, "questions.json")));
            Assert.True(File.Exists(Path.Combine(dir, "acls.json")));
            Assert.True(Directory.Exists(examplesDir));
            Assert.True(File.Exists(Path.Combine(examplesDir, "users.example.json")));
            Assert.False(File.Exists(Path.Combine(dir, "users.json")));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void Reset_WhenLocalFilesDoNotExist_DoesNotFail()
    {
        var dir = CreateTempDataDir();
        try
        {
            var service = new DataResetService(dir);
            var removed = service.ResetLocalData();

            Assert.Empty(removed);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    private static string CreateTempDataDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "NetLearnBattleResetTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
